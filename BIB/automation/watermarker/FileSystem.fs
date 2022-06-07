﻿module FileSystem

open Model

let private workingDir = "c:/github/jamesfaix/eccentria/bib/watermarks"

let private escapeSetCode = function
    | "con" -> "conflux" // Can't call a file 'con' on Windows
    | x -> x
    
let svgPath (code: string) =
    $"{workingDir}/{escapeSetCode code}.svg"

let private serialize = function
    | White -> "white"
    | Blue -> "blue"
    | Black -> "black"
    | Red -> "red"
    | Green -> "green"
    | Colorless -> "colorless"
    | Gold -> "gold"
    | LandColorless -> "land-colorless"

let backgroundPath (color: WatermarkColor) =
    $"{workingDir}/background-{serialize color}.png"

let watermarkPath (code: string) (color: WatermarkColor) =
    $"{workingDir}/{escapeSetCode code}-{serialize color}.png"
