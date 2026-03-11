using System;
using System.Collections.Generic;
using System.Linq;

namespace RobloxApiDumpTool
{
    public class DiffChangeList : List<object>
    {
        public string Name { get; private set; }
        public string Prefix { get; private set; }

        public DiffChangeList(string name = "ChangeList", string prefix = "")
        {
            Name = name;
            Prefix = prefix;
        }

        public string ListElements(string separator, string prefix = "")
        {
            string[] elements = this
                .Select(elem => prefix + elem.ToString())
                .ToArray();

            return string.Join(separator, elements);
        }

        public override string ToString()
        {
            return ListElements(" ");
        }

        public void WriteHtml(ReflectionHtml html, bool multiline = false)
        {
            var writeCell = new Action<object, object>((change, prevChange) =>
            {
                if (change is Parameters parameters)
                {
                    parameters.WriteHtml(html, true);
                }
                else if (change is LuaType type)
                {
                    if (prevChange is Parameters)
                        html.Symbol("-> ");

                    type.WriteHtml(html);
                }
                else if (change is Descriptor desc)
                {
                    desc.WriteHtml(html);
                }
                else
                {
                    string value;
                    string tagClass;

                    if (change is Security security)
                    {
                        tagClass = "Security";

                        if (security.Type == SecurityType.None)
                            tagClass += " darken";

                        value = security.Describe(true);
                    }
                    else if (change is ReadWriteSecurity rwSecurity)
                    {
                        tagClass = "Security";

                        if (rwSecurity.Merged && rwSecurity.Read.Type == SecurityType.None)
                            tagClass += " darken";

                        value = rwSecurity.Describe(true);
                    }
                    else if (change is Capabilities capabilities)
                    {
                        tagClass = "Capabilities";

                        if (capabilities.IsEmpty())
                            tagClass += " darken";

                        value = capabilities.Describe(true);
                    }
                    else
                    {
                        if (change is ThreadSafety)
                            tagClass = "ThreadSafety";
                        else if (change is Serialization)
                            tagClass = "Serialization";
                        else
                            tagClass = change.GetType().Name;

                        value = change.ToString();

                        if (value.StartsWith("\""))
                        {
                            html.String(value);
                            return;
                        }
                    }

                    html.Span(tagClass, value);
                }
            });

            var buildChangeList = new Action(() =>
            {
                object prevChange = null;

                if (multiline)
                {
                    html.OpenStack("span", "ChangeListPrefix", () =>
                    {
                        html.Text(Prefix);
                        html.Symbol(": ");
                    });
                }
                else
                {
                    html.Text($" {Prefix.Trim()} ");
                }
                
                foreach (object change in this)
                {
                    var writeItem = new Action(() =>
                    {
                        writeCell(change, prevChange);
                        prevChange = change;
                    });

                    if (multiline)
                    {
                        html.OpenStack("span", "ChangeListItem", writeItem);
                        continue;
                    }

                    writeItem();
                }
            });

            if (multiline)
            {
                html.OpenStack("div", "ChangeList", buildChangeList);
                return;
            }
            
            html.OpenSpan(Name, buildChangeList);
        }
    }
}