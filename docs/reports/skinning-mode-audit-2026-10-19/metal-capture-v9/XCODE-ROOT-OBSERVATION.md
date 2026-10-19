# Root Xcode observation (2026-10-19)

Root inspected the actual V9 bundle with Xcode26.6. The Open panel recognized it as GPUTraceDocumentType, displayed18KB, but Open was disabled. No Metal commands were read. Together with deviceId0 and the8-byte capture stream, this capture is rejected as command/dispatch evidence. This is not a conclusion about GPU skinning support. Root's separate Finder/CUA cgwindow error is unrelated to capture validity and is not a capture diagnosis.

No V9 retry/source rewrite follows from this observation. V10 is separately prepared to test explicit Begin/End with completed GPU readbacks, under a fresh version/source identity and root window.
