module FileSystem

open Model

let private workingDir = "c:/github/jamesfaix/eccentria/bib/watermarks"

let svgPath (code: string) =
    $"{workingDir}/{code}.svg"

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
    $"{workingDir}/{code}-{serialize color}.png"
