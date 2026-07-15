| File | OpenJPEG arguments |
| --- | --- |
| `images-jpeg2000-indexed-lossless-53.pdf` | `-F 1000,1043,1,8,u -r 1 -mct 0` |
| `images-jpeg2000-indexed-jp2-palette.pdf` | `-F 1000,1043,1,8,u -r 1 -mct 0` |
| `images-jpeg2000-gray8-lossless-53.pdf` | `-r 1` |
| `images-jpeg2000-gray8-lossy-97-20x.pdf` | `-I -r 20` |
| `images-jpeg2000-ycc8-lossless-53.pdf` | `-r 1` |
| `images-jpeg2000-ycc8-signed.pdf` | `-F 1000,1043,3,8,s@1x1:1x1:1x1 -r 1 -mct 1` (planar signed RGB input, each source JPEG sample shifted by -128) |
| `images-jpeg2000-rgb8-lossy-97.pdf` | `-I -r 20 -mct 0` |
| `images-jpeg2000-rgb8-component-coc.pdf` | `-I -r 20 -mct 0 -fixture-comp-coc` (patched OpenJPEG encoder) |
| `images-jpeg2000-rgb8-component-coc-qcc.pdf` | `-I -r 20 -mct 0 -fixture-comp-coc` (patched OpenJPEG encoder) |
| `images-jpeg2000-ycc8-scalar-derived-qnt.pdf` | `-I -r 20 -mct 0 -fixture-scalar-derived-qnt` (patched OpenJPEG encoder) |
| `images-jpeg2000-ycc8-colorspace.pdf` | `-F 1000,1043,3,8,u -I -r 20 -mct 0` (raw YCbCr samples, JP2 `colr` enum 18) |
| `images-jpeg2000-ycc8-lossy-97-40-20-10.pdf` | `-I -r 40,20,10` |
| `images-jpeg2000-ycc8-pdf-bpc-4.pdf` | `-I -r 40,20,10` |
| `images-jpeg2000-ycc8-pdf-nobpc.pdf` | `-I -r 40,20,10` |
| `images-jpeg2000-ycc8-pdf-decode.pdf` | `-I -r 40,20,10` |
| `images-jpeg2000-ycc8-psnr-layers.pdf` | `-I -q 30,35,40,45` |
| `images-jpeg2000-ycc8-prog-lrcp.pdf` | `-I -r 40,20,10 -p LRCP -n 5` |
| `images-jpeg2000-ycc8-prog-rlcp.pdf` | `-I -r 40,20,10 -p RLCP -n 5` |
| `images-jpeg2000-ycc8-prog-rpcl.pdf` | `-I -r 40,20,10 -p RPCL -n 5` |
| `images-jpeg2000-ycc8-prog-pcrl.pdf` | `-I -r 40,20,10 -p PCRL -n 5` |
| `images-jpeg2000-ycc8-prog-cprl.pdf` | `-I -r 40,20,10 -p CPRL -n 5` |
| `images-jpeg2000-ycc8-tiled-128.pdf` | `-r 20,10,1 -t 128,128` |
| `images-jpeg2000-ycc8-tiled-offsets.pdf` | `-r 20,10,1 -t 128,128 -d 13,17 -T 5,7` |
| `images-jpeg2000-ycc8-precinct-codeblock.pdf` | `-I -r 40,20,10 -n 6 -c [128,128] -b 32,32` |
| `images-jpeg2000-ycc8-small-codeblock.pdf` | `-I -r 40,20,10 -n 5 -c [64,64] -b 16,16` |
| `images-jpeg2000-ycc8-sop-eph.pdf` | `-I -r 20 -SOP -EPH` |
| `images-jpeg2000-ycc8-plt.pdf` | `-I -r 20 -PLT` |
| `images-jpeg2000-ycc8-tileparts-components.pdf` | `-I -r 40,20,10 -TP C` |
| `images-jpeg2000-ycc8-bypass.pdf` | `-I -r 20 -M 1` |
| `images-jpeg2000-ycc8-vsc.pdf` | `-I -r 20 -M 8` |
| `images-jpeg2000-ycc8-erterm.pdf` | `-I -r 20 -M 16` |
| `images-jpeg2000-ycc8-reset-restart-segmark.pdf` | `-I -r 20 -M 38` |
| `images-jpeg2000-ycc8-roi-component1.pdf` | `-r 20,10,1 -ROI c=1,U=20` |
| `images-jpeg2000-ycc8-rgn-roi.pdf` | `-r 20,10,1 -ROI c=1,U=20` |
| `images-jpeg2000-ycc8-poc-cprl.pdf` | `-I -r 40,20,10 -n 5 -p LRCP -POC T0=0,0,1,5,3,CPRL` |
| `images-jpeg2000-ycc8-poc-multi.pdf` | `-r 4,2,1 -n 6 -p LRCP -POC T1=0,0,3,3,3,RLCP/T1=3,0,3,6,3,CPRL` |
| `images-jpeg2000-ycc8-subsample-2x2.pdf` | `-r 20 -s 2,2` |
| `images-jpeg2000-gray16-lossless.pdf` | `-r 1` |
| `images-jpeg2000-gray16-lossy-97.pdf` | `-I -r 20` |
| `images-jpeg2000-rgba8-alpha.pdf` | `-r 20,10,1` |
| `images-jpeg2000-cmyk8-lossless.pdf` | `-F 1000,1043,4,8,u -r 1 -mct 0` |
| `images-jpeg2000-cmyk8-lossy-97.pdf` | `-F 1000,1043,4,8,u -I -r 20 -mct 0` |
| `images-jpeg2000-cmyk8-layers.pdf` | `-F 1000,1043,4,8,u -I -r 80,40,20,10 -mct 0` |
| `images-jpeg2000-cmyk8-tiled.pdf` | `-F 1000,1043,4,8,u -I -r 40,20,10 -t 128,128 -mct 0` |
| `images-jpeg2000-cmyk8-precincts.pdf` | `-F 1000,1043,4,8,u -I -r 40,20,10 -n 5 -c [128,128] -b 32,32 -mct 0` |
| `images-jpeg2000-cmyk8-prog-cprl.pdf` | `-F 1000,1043,4,8,u -I -r 40,20,10 -p CPRL -n 5 -mct 0` |
| `images-jpeg2000-cmyk8-sop-eph.pdf` | `-F 1000,1043,4,8,u -I -r 20 -SOP -EPH -mct 0` |
| `images-jpeg2000-jp2.pdf` | `-I` |
