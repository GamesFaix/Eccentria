module Model

type CardColor = White | Blue | Black | Red | Green

type CardType = Spell | Land

type CardColors = 
    | Colorless
    | One of CardColor
    | Two of CardColor * CardColor
    | Multi

type WatermarkType = CardType * CardColors
   