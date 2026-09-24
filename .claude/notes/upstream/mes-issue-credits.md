# Title
ChangeNpcFactionCredits ignores ChangeNpcFactionCreditsAmount, and credit deductions never fail

# Body

Found two problems in the credit actions in `ActionSystem.cs` (MES 2.74.02).

**1. `ChangeNpcFactionCredits` reads the player amount**

`[ChangeNpcFactionCreditsAmount:]` is parsed (`ActionReferenceProfile.cs` line 1289) but never read. The faction block uses the player amount instead:

```csharp
// ActionSystem.cs line 1041, inside the ChangeNpcFactionCredits block
long changePlayerCreditsAmount = actions.ChangePlayerCreditsAmount;
```

So this does nothing:

```
[ChangeNpcFactionCredits:true]
[ChangeNpcFactionCreditsTag:GRAY]
[ChangeNpcFactionCreditsAmount:50000000]
```

and you have to use `[ChangePlayerCreditsAmount:50000000]` instead. That also means one action can't give the player and the faction different amounts.

**2. Deductions never fail**

All three credit blocks (player loop ~938, saved player ~993, NPC faction ~1063) check for insufficient funds like this:

```csharp
if (changePlayerCreditsAmount > 0) {
    // add
} else {
    if (changePlayerCreditsAmount > credits)   // amount is negative here, so this is never true
        PaymentFailureTriggered = true;
    else {
        RequestChangeBalance(changePlayerCreditsAmount);
        PaymentSuccessTriggered = true;
    }
}
```

In the else branch the amount is zero or negative, so `amount > credits` is false for any non-negative balance. Charging a player 1,000,000 when they have 10 goes straight to `RequestChangeBalance` and fires `PaymentSuccess`, and `PaymentFailure` can never fire from a charge.

**Proposed fix**

For 1, use the NPC amount when it's set and fall back to the player amount otherwise, so existing profiles that rely on the current behavior keep working:

```csharp
long changePlayerCreditsAmount = actions.ChangeNpcFactionCreditsAmount != 0
    ? actions.ChangeNpcFactionCreditsAmount
    : actions.ChangePlayerCreditsAmount;
```

If you want a counter override for the faction amount too, it could get its own `ChangeNpcFactionCreditsAmountCounter`, but that's optional.

For 2, compare the size of the charge in all three places:

```csharp
if (-changePlayerCreditsAmount > credits)
    PaymentFailureTriggered = true;
```

Changing the check means charges that currently always go through will start failing when the balance is too low. That's the documented intent of `PaymentFailure`, but it could change how existing mods behave, so it might be worth a line in the patch notes.
