// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jbig2.Coding
{
    internal class JbigArithmeticContexts
    {
        private JbigArithmeticContext? iadt;
        public JbigArithmeticContext IADT => iadt ??= new JbigArithmeticContext(512);

        private JbigArithmeticContext? iafs;
        public JbigArithmeticContext IAFS => iafs ??= new JbigArithmeticContext(512);

        private JbigArithmeticContext? iads;
        public JbigArithmeticContext IADS => iads ??= new JbigArithmeticContext(512);

        private JbigArithmeticContext? iardw;
        public JbigArithmeticContext IARDW => iardw ??= new JbigArithmeticContext(512);

        private JbigArithmeticContext? iardh;
        public JbigArithmeticContext IARDH => iardh ??= new JbigArithmeticContext(512);

        private JbigArithmeticContext? iardx;
        public JbigArithmeticContext IARDX => iardx ??= new JbigArithmeticContext(512);

        private JbigArithmeticContext? iardy;
        public JbigArithmeticContext IARDY => iardy ??= new JbigArithmeticContext(512);

        private JbigArithmeticContext? iait;
        public JbigArithmeticContext IAIT => iait ??= new JbigArithmeticContext(512);

        private JbigArithmeticContext? iari;
        public JbigArithmeticContext IARI => iari ??= new JbigArithmeticContext(512);

        private JbigArithmeticContext? iaex;
        public JbigArithmeticContext IAEX => iaex ??= new JbigArithmeticContext(512);

        private JbigArithmeticContext? iaai;
        public JbigArithmeticContext IAAI => iaai ??= new JbigArithmeticContext(512);

        private JbigArithmeticContext? iadw;
        public JbigArithmeticContext IADW => iadw ??= new JbigArithmeticContext(512);

        private JbigArithmeticContext? iadh;
        public JbigArithmeticContext IADH => iadh ??= new JbigArithmeticContext(512);

        private JbigArithmeticContext? gr;
        public JbigArithmeticContext GR => gr ??= new JbigArithmeticContext(1 << 13);

        private JbigArithmeticContext? gb;
        public JbigArithmeticContext GB => gb ??= new JbigArithmeticContext(1 << 16);

        private JbigArithmeticContext? iaid;
        public JbigArithmeticContext IAID => iaid ??= new JbigArithmeticContext(512);

        public void Restore(JbigArithmeticContext gb, JbigArithmeticContext gr)
        {
            this.gb = gb;
            this.gr = gr;
        }
    }
}
