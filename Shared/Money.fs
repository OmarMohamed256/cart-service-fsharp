module Cart.Shared.Money

/// Represents a non-negative money amount
type Money = private Money of decimal

module Money =
    let create value =
        if value < 0m then Error "Money cannot be negative"
        else Ok (Money value)

    let value (Money v) = v