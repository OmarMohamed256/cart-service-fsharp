# cart-service-fsharp

A simple **F# shopping cart application** demonstrating **functional domain modeling**, **functional dependency injection**, and **Oxpecker** for HTTP endpoints.  

---

## Features

- **Functional Domain Modeling**  
  Clean modeling of `Cart`, `CartItem`, `Product`, `Quantity`, and `Money` as domain types with smart constructors.  

- **Functional Dependency Injection**  
  Services are injected via interfaces (`IGetCart`, `ISaveCartItem`, `IGetProduct`) and wrapped in `OperationEnv`.  

- **Web API with Oxpecker**  
  Minimal, F#-friendly HTTP routing for cart operations:  
  - `GET /cart/{cartId}` – get a cart  
  - `GET /cart/{cartId}/total` – get cart total  
  - `POST /cart/{cartId}` – add an item to cart  

- **Error handling**  
  Structured 400, 404, and 500 error responses with logging.  

- **Serialization-safe DTOs**  
  Converts F# discriminated unions to plain DTOs for JSON serialization.  

---

## Domain Model

- `Cart` – represents a shopping cart  
- `CartItem` – an item in the cart  
- `Product` – product entity  
- `Money` – non-negative decimal type with smart constructor  
- `Quantity` – non-negative integer type with smart constructor  

---

## Technologies

- [F#](https://fsharp.org/)  
- [Oxpecker](https://github.com/Zaid-Ajaj/Oxpecker) – lightweight F# web framework  
- [PostgreSQL](https://www.postgresql.org/) – relational database  
- [Npgsql.FSharp](https://fsprojects.github.io/Npgsql.FSharp/) – F#-friendly PostgreSQL client


