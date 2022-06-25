module FileSystem

open Model

let private workingDir = "c:/github/jamesfaix/eccentria/bbb/watermarks"

let private escapeSetCode = function
    | "con" -> "conflux" // Can't call a file 'con' on Windows
    | x -> x
    
let svgPath (code: string) =
    $"{workingDir}/{escapeSetCode code}.svg"

let maskPath (code: string) =
    $"{workingDir}/{escapeSetCode code}-mask.png"

let private serializeColor = function
    | White -> "w"
    | Blue -> "u"
    | Black -> "b"
    | Red -> "r"
    | Green -> "g"

let private serializeCardType = function
    | Spell -> ""
    | Land -> "land-"

let private serializeColors = function
    | Colorless -> "c"
    | One x -> serializeColor x
    | Two (x, y) -> serializeColor x + serializeColor y
    | Multi -> "m"

let serializeWatermark (w : WatermarkType) =
    let cardType, colors = w
    serializeCardType cardType + serializeColors colors
    
let backgroundPath (w: WatermarkType) =
    $"{workingDir}/background-{serializeWatermark w}.png"

let watermarkPath (code: string) (w: WatermarkType) =
    $"{workingDir}/{escapeSetCode code}-{serializeWatermark w}.png"

let scryfallSetsDataPath () =
    $"{workingDir}/data/scryfall-sets.json"

let scryfallCardsDataPath () =
    $"{workingDir}/data/scryfall-cards.json"

let mtgdCardsDataPath () =
    $"{workingDir}/data/mtgd-cards.json"