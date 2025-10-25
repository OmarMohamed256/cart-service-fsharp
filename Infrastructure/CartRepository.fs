// In Cart.Infrastructure/CartRepository.fs
namespace Cart.Infrastructure

open System.Threading.Tasks
open Cart.Domain.Cart
open Cart.Domain.Product
open Cart.Shared.Money
open Npgsql.FSharp

module ProductRepository =
    let getProduct (connectionString: string) (ProductId productId): Task<Product option> = task {
        let! rows =
            Sql.connect connectionString
            |> Sql.query "SELECT id, name, unit_price FROM products WHERE id = @productId"
            |> Sql.parameters [ "productId", Sql.uuid productId ]
            |> Sql.executeAsync (fun read ->
                match Money.create (read.decimal "unit_price") with
                | Ok price ->
                    let id = ProductId(read.uuid "id")
                    let name = read.string "name"
                    { Id = id; Name = name; UnitPrice = price }
                | Error e -> failwith $"Invalid money value in database: {e}"
            )

        match rows with
        | [] -> return None
        | [product] -> return Some product
        | _ -> return failwith "Multiple products found with the same ID"
    }


// This module now contains the concrete data access implementations
module CartRepository =

    // The function now takes the string directly
    let getCart (connectionString: string) (CartId cartId) : Task<Cart option> = task {
        let! rows =
            Sql.connect connectionString
            |> Sql.query "SELECT cart_id, product_id, quantity, unit_price FROM cart_items WHERE cart_id = @cart_id"
            |> Sql.parameters [ "cart_id", Sql.uuid cartId ]
            |> Sql.executeAsync (fun read ->
                match Quantity.create (read.int "quantity"), Money.create (read.decimal "unit_price") with
                | Ok qty, Ok price ->
                    { ProductId = ProductId(read.uuid "product_id"); Quantity = qty; UnitPrice = price }
                | Error e, _ -> failwith $"Invalid quantity: {e}"
                | _, Error e -> failwith $"Invalid money value: {e}"
            )

        
        match rows with
        | [] -> return None
        | items ->
            let cart = { Id = CartId cartId; Items = items }
            return Some cart
    }

    // This function also takes the string directly
    let saveCartItem (connectionString: string) (CartId cartId) (item: CartItem) : Task<int> =
        let (ProductId productId) = item.ProductId
        let quantity = Quantity.value item.Quantity
        let unitPrice = Money.value item.UnitPrice

        let sql = """
            INSERT INTO cart_items (cart_id, product_id, quantity, unit_price)
            VALUES (@cart_id, @product_id, @quantity, @unit_price)
            ON CONFLICT (cart_id, product_id) 
            DO UPDATE SET
                quantity = @quantity,
                unit_price = @unit_price;
        """

        Sql.connect connectionString // Use the passed-in string
        |> Sql.query sql
        |> Sql.parameters [
            "cart_id", Sql.uuid cartId
            "product_id", Sql.uuid productId
            "quantity", Sql.int quantity
            "unit_price", Sql.decimal unitPrice
        ]
        |> Sql.executeNonQueryAsync