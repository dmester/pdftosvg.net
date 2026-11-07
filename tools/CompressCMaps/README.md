# CompressCMaps

This tool makes a binary compressed representation of the predefined CMaps allowed in PDFs. It produces a
single compressed file instead of one file per CMap to provide better compression ratio. This will have a
negative performance impact on usage, but PDFs using predefined CMaps seem to be quite rare.

The tool packs CID, notdef and BF character and range mappings. It optimizes CID and BF mappings before compression.

## Usage

1. Download the source files from https://github.com/adobe-type-tools/cmap-resources and
   https://github.com/adobe-type-tools/mapping-resources-pdf. Place them under a common resource directory with
   `license.md` at its root.

2. Optionally download a Windows binary of Zopfli and put it in the output folder of CompressCMaps. This will improve compression of the resulting file.

3. Run:

   ```
   CompressCMaps <path to CMap resource directory> <output file>
   ```
