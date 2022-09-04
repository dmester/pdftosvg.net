# CompressCMaps

This tool makes a binary compressed representation of the predefined CMaps allowed in PDFs. It produces a
single compressed file instead of one file per CMap to provide better compression ratio. This will have a
negative performance impact on usage, but PDFs using predefined CMaps seem to be quite rare.

Note that the tool only handles CID and Not Def chars and ranges, and will do optimizations of the data
before it is compressed. BF chars and ranges are discarded.

## Usage

1. Download source CMaps from https://github.com/adobe-type-tools/cmap-resources

2. Optionally download a Windows binary of Zopfli and put it in the output folder of CompressCMaps. This will improve compression of the resulting file.

3. Run:

   ```
   CompressCMaps <path to cmap-resources folder> <output file>
   ```
