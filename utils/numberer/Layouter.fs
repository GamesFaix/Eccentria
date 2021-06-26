﻿module Layouter

open Model

let private getStyleTag (heightInches: float, widthInches: float) : string =
    sprintf "<style> img { height: %fin; width: %fin; }</style>" heightInches widthInches

let private getImageTag (card: CardInfo) : string =
    sprintf "<img src=\"%s\"/>" (FileReaderWriter.getCardFileName card)

let createHtmlLayout (cards : CardInfo list) : string =
    let styleTag = getStyleTag (3.5, 2.5)
    let cardTags = cards |> List.map getImageTag
    styleTag::cardTags 
    |> String.concat "\n"