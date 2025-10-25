module Cart.Handlers

open System
open System.Threading.Tasks
open Cart.Domain.Cart
open Cart.Domain.Product
open Cart.Infrastructure
open Cart.Services
open Cart.Abstractions
open Cart.Services.CartService
open Cart.Shared.Money
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Oxpecker
open type Microsoft.AspNetCore.Http.TypedResults

/// <summary>
/// This "wrapper" type implements the repository interfaces required by CartService.
/// It takes the global 'env' and delegates calls to the concrete repository
/// functions, passing in the dependencies from 'env' (like the connection string).
/// </summary>
type OperationEnv<'T when 'T :> ICartDbEnv and 'T :> IAppLogger>(env: 'T) =
    interface ICartDbEnv with
        member _.ConnectionString = env.ConnectionString
    interface IAppLogger with
        member _.Logger = env.Logger

    interface IGetCart with
        member _.GetCart cartId = CartRepository.getCart env.ConnectionString cartId
    interface ISaveCartItem with
        member _.SaveCartItem (cartId, item) = CartRepository.saveCartItem env.ConnectionString cartId item
    interface IGetProduct with
        member _.GetProduct productId = ProductRepository.getProduct env.ConnectionString productId

            
type AddItemDto = { ProductId: Guid; Quantity: int }
type CartItemDto = {
    ProductId: Guid
    Quantity: int
    UnitPrice: decimal
}

type CartDto = {
    Id: Guid
    Items: CartItemDto list
}

// Converts domain CartItem -> CartItemDto
let toCartItemDto (item: CartItem) =
    {
        ProductId = let (ProductId id) = item.ProductId in id
        Quantity = Quantity.value item.Quantity
        UnitPrice = Money.value item.UnitPrice
    }

// Converts domain Cart -> CartDto
let toCartDto (cart: Cart) =
    {
        Id = let (CartId id) = cart.Id in id
        Items = cart.Items |> List.map toCartItemDto
    }
/// <summary>
/// The Oxpecker HTTP handlers for the Cart API.
/// </summary>
module CartHandlers =

    let getCart env cartId (ctx: HttpContext) =
        task {
            let operationEnv = OperationEnv(env)
            let! cartOpt = CartService.getCart operationEnv (CartId cartId)
            match cartOpt with
            | Some cart -> return! ctx.Write <| Ok (toCartDto cart)
            | None -> return! ctx.Write <| NotFound {| Error = "Cart not found" |}
        } :> Task

    let addItemToCart env cartId (ctx: HttpContext) =
        task {
            let operationEnv = OperationEnv(env)
            let! dto = ctx.BindJson<AddItemDto>()
            let cmd: AddItemToCartCommand = {
                CartId = CartId cartId
                ProductId = ProductId dto.ProductId
                Quantity = dto.Quantity
            }
            let! result = CartService.addItemToCart operationEnv cmd
            match result with
            | Ok () -> return! ctx.Write <| Ok {| Message = "Item added to cart" |}
            | Error msg ->
                env.Logger.LogWarning("Failed to add item to cart {CartId}: {Error}", cartId, msg)
                return! ctx.Write <| BadRequest {| Error = msg |}
        } :> Task

    let getCartTotal env cartId (ctx: HttpContext) =
        task {
            let operationEnv = OperationEnv(env)
            let! result = CartService.getCartTotal operationEnv (CartId cartId)
            match result with
            | Ok total ->
                return! ctx.Write <| Ok {| Total = Money.value total; Currency = "USD" |}
            | Error msg ->
                env.Logger.LogWarning("Failed to get total for cart {CartId}: {Error}", cartId, msg)
                return! ctx.Write <| NotFound {| Error = msg |}
        } :> Task