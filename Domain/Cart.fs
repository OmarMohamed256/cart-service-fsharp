module Cart.Domain.Cart

open Cart.Domain.Product
open Cart.Shared.Money

type CartId = CartId of System.Guid
type Quantity = private Quantity of int

module Quantity =
    let create q = if q > 0 then Ok (Quantity q) else Error "Quantity must be positive"
    let value (Quantity q) = q
    
type CartItem = {
    ProductId: ProductId
    Quantity: Quantity
    UnitPrice: Money
}

module CartItem =
    let total item =
        let q = Quantity.value item.Quantity |> decimal
        let unit = Money.value item.UnitPrice
        Money.create (q * unit)
        
type Cart = {
    Id: CartId
    Items: CartItem list
}

module Cart =
    let total cart =
        cart.Items
        |> List.map CartItem.total
        |> List.fold (fun acc res ->
            match acc, res with
            | Ok sum, Ok m -> Money.create (Money.value sum + Money.value m)
            | Error e, _ | _, Error e -> Error e
        ) (Money.create 0m)