module Cart.Domain.Product

open Cart.Shared.Money

type ProductId = ProductId of System.Guid
type Product = {
    Id: ProductId
    Name: string
    UnitPrice: Money
}