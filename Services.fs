module Cart.Services

open System.Threading.Tasks
open Cart.Domain.Cart
open Cart.Domain.Product

type IGetCart =
    abstract member GetCart: CartId -> Task<Cart option>

type ISaveCartItem =
    abstract member SaveCartItem: CartId * CartItem -> Task<int>

type IGetProduct =
    abstract member GetProduct: ProductId -> Task<Product option>
    
    
module CartService =
    let getCart (env: #IGetCart) (cartId: CartId)= env.GetCart cartId
    
    type AddItemToCartCommand = {
        CartId: CartId
        ProductId: ProductId
        Quantity: int
    }
    
    let addItemToCart (env: #IGetCart & #ISaveCartItem & #IGetProduct) (cmd: AddItemToCartCommand) =
        task {
            let! productOpt = env.GetProduct cmd.ProductId
            match productOpt with
            | None -> return Error "Product not found"
            | Some product ->
                match Quantity.create cmd.Quantity with
                | Ok qty ->
                    let item = { 
                        ProductId = cmd.ProductId
                        Quantity = qty
                        UnitPrice = product.UnitPrice 
                    }
                    
                    let! _ = env.SaveCartItem(cmd.CartId, item)
                    return Ok ()
                        
                | Error e -> return Error e
        }
        
    let getCartTotal (env: #IGetCart) (cartId: CartId) =
        task {
            let! cartOpt = env.GetCart cartId
            match cartOpt with
            | None -> return Error "Cart not found"
            | Some cart -> 
                return Cart.total cart
        }
