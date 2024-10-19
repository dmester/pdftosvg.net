// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg
{
    internal class OptionalContentGroupManager
    {
        private Dictionary<PdfDictionary, OptionalContentGroup> allGroups = new();
        private List<OptionalContentGroup> publicGroups = new();
        private PdfName viewerIntent = Names.View;

        public OptionalContentGroupCollection PublicGroups { get; }

        public OptionalContentGroupManager() 
        {
            PublicGroups = new OptionalContentGroupCollection(publicGroups);
        }

        public void Initialize(PdfDictionary rootDict)
        {
            if (!rootDict.TryGetDictionary(Names.OCProperties, out var ocProperties) ||
                !ocProperties.TryGetArray<PdfDictionary>(Names.OCGs, out var ocgs))
            {
                return;
            }

            foreach (var ocg in ocgs)
            {
                var name = ocg.GetValueOrDefault(Names.Name, PdfString.Empty).ToString();
                allGroups[ocg] = new OptionalContentGroup(name);
            }

            if (ocProperties.TryGetDictionary(Names.D, out var d))
            {
                if (d.TryGetArray<PdfDictionary>(Names.Order, out var order))
                {
                    foreach (var ocgDict in order)
                    {
                        if (allGroups.TryGetValue(ocgDict, out var ocg))
                        {
                            publicGroups.Add(ocg);
                        }
                    }
                }

                InitializeState(d);
            }
        }

        public bool ApplicableIntent(PdfDictionary dict)
        {
            if (dict.TryGetValue(Names.Intent, out var intent))
            {
                if (intent is PdfName intentName)
                {
                    return viewerIntent == intentName;
                }
                else if (intent is object[] array)
                {
                    // ISO-32000-2 section 8.11.2.3
                    // Empty Intent array should not affect visibility
                    return array
                        .OfType<PdfName>()
                        .Any(intentItem => viewerIntent == intentItem);
                }
            }

            // Default intent is View
            return viewerIntent == Names.View;
        }

        public bool GroupVisible(PdfDictionary dict)
        {
            if (!ApplicableIntent(dict))
            {
                // Groups with non-applicable intent should not affect visibility
                return true;
            }

            if (allGroups.TryGetValue(dict, out var ocg))
            {
                return ocg.Visible;
            }

            if (dict.GetNameOrNull(Names.Type) == Names.OCMD)
            {
                if (dict.TryGetArray(Names.VE, out var ve))
                {
                    return EvaluateVisibilityExpression(ve);
                }

                if (dict.TryGetArray(Names.OCGs, out var ocgDicts))
                {
                    var visible = ocgDicts
                        .OfType<PdfDictionary>()
                        .Select(ocgDict => allGroups.TryGetValue(ocgDict, out var ocg) ? ocg.Visible : (bool?)null)
                        .WhereNotNull();

                    var policy = dict.GetNameOrNull(Names.P);

                    if (policy == Names.AnyOff)
                    {
                        return visible.DefaultIfEmpty(false).Any(value => !value);
                    }
                    else if (policy == Names.AllOff)
                    {
                        return visible.All(value => !value);
                    }
                    else if (policy == Names.AllOn)
                    {
                        return visible.All(value => value);
                    }
                    else // Default: Names.AnyOn
                    {
                        return visible.DefaultIfEmpty(true).Any(value => value);
                    }
                }
            }

            return true;
        }

        private bool EvaluateVisibilityExpression(object expression)
        {
            if (expression is object?[] arr && arr.Length > 0 && arr[0] is PdfName op)
            {
                if (op == Names.Not)
                {
                    if (arr.Length > 1 &&
                        arr[1] is PdfDictionary ocgDict &&
                        allGroups.TryGetValue(ocgDict, out var ocg))
                    {
                        return !ocg.Visible;
                    }

                    return true;
                }

                if (op == Names.And)
                {
                    foreach (var arg in arr.Skip(1))
                    {
                        if (arg is object[] argArr && !EvaluateVisibilityExpression(argArr))
                        {
                            return false;
                        }
                        else if (arg is PdfDictionary ocgDict &&
                            allGroups.TryGetValue(ocgDict, out var ocg) &&
                            !ocg.Visible)
                        {
                            return false;
                        }
                    }

                    return true;
                }

                if (op == Names.Or)
                {
                    foreach (var arg in arr.Skip(1))
                    {
                        if (arg is object[] argArr && EvaluateVisibilityExpression(argArr))
                        {
                            return true;
                        }
                        else if (arg is PdfDictionary ocgDict &&
                            allGroups.TryGetValue(ocgDict, out var ocg) &&
                            ocg.Visible)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            return true;
        }

        private void InitializeState(PdfDictionary configDict)
        {
            var baseState = true;
            
            if (configDict.TryGetName(Names.BaseState, out var baseStateValue) && baseStateValue == Names.OFF)
            {
                baseState = false;
            }

            void SetVisibility(PdfName key, bool visibility)
            {
                if (configDict.TryGetArray<PdfDictionary>(key, out var ocgDicts))
                {
                    foreach (var ocgDict in ocgDicts)
                    {
                        if (allGroups.TryGetValue(ocgDict, out var ocg))
                        {
                            ocg.Visible = visibility;
                        }
                    }
                }
            }

            void SetState(PdfNamePath statePath)
            {
                foreach (var group in allGroups)
                {
                    var state = group.Key.GetNameOrNull(statePath);
                    if (state != null)
                    {
                        if (state == Names.ON)
                        {
                            group.Value.Visible = true;
                        }
                        else if (state == Names.OFF)
                        {
                            group.Value.Visible = false;
                        }
                    }
                }
            }

            // BaseState
            foreach (var ocg in allGroups.Values)
            {
                ocg.Visible = baseState;
            }

            // ON/OFF
            if (baseState)
            {
                SetVisibility(Names.OFF, false);
            }
            else
            {
                SetVisibility(Names.ON, true);
            }

            // ViewState / ExportState
            SetState(Names.Usage / Names.View / Names.ViewState);
            SetState(Names.Usage / Names.Export / Names.ExportState);
        }
    }
}
