# Unity selection screen: prototype visual pass

The landscape selection screen now follows the approved prototype composition: navy radial backdrop, warm double halo, elliptical platform, italic display typography, portrait roster, arena image card, cyan stat lines, amber start action and right-side settings/arena sheets. Combat numbers remain actual shared mechanics instead of the prototype's fictional power rating.

The preview camera renders to an alpha RenderTexture composited below the UI. Drag/pinch controls remain. BeginFight detaches/releases that texture and restores direct full-screen camera rendering; destruction also releases generated textures. Headless tests avoid allocating GPU targets.

Resources/Selection contains eight locally rendered roster thumbnails and five arena previews from the prototype. This does not upgrade the six procedural placeholder characters. Dance remains explicitly unavailable; no fake dance animation is presented.

Reproducible visual review: LighFight.Editor.SelectionScreenCapture.Run. Captures setup, rules, arenas and transition to battle in art/previews/selection-unity. This editor-only tool disables asynchronous shader compilation so screenshots do not capture temporary compilation placeholder colors.

The desktop visual review is not an iPhone device validation. Existing battle HUD is outside this selection-screen visual pass.
