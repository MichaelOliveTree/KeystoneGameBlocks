using System;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using System.Collections;

namespace KeyEdit.Scripting
{

        // GListBox class 
        public class IntellisenseListBox : ListBox
        {
            private ImageList imageList;
            public ImageList ImageList
            {
                get { return imageList; }
                set { imageList = value; }
            }

            public IntellisenseListBox()
            {
                // Set owner draw mode
                this.DrawMode = DrawMode.OwnerDrawFixed;
                this.ItemHeight = 16;
                this.Font = new Font(FontFamily.GenericSansSerif, 7f);

                this.Click += new EventHandler(IntellisenseListBox_Click);
                this.DoubleClick += new EventHandler(IntellisenseListBox_DoubleClick);
                this.Cursor = Cursors.Default;

                this.BorderStyle = BorderStyle.FixedSingle;
            }

            void IntellisenseListBox_DoubleClick(object sender, EventArgs e)
            {
                ((ScriptEditorControl)Parent).ConfirmIntellisenseBox();
            }

            void IntellisenseListBox_Click(object sender, EventArgs e)
            {
                Parent.Focus();
            }

            public void Populate(Type type)
            {
                this.Items.Clear();

                ArrayList typeItems = new ArrayList();

                MemberInfo[] memberInfo = type.GetMembers();

                for (int i = 0; i < memberInfo.Length; i++)
                {
                    if (memberInfo[i].ReflectedType.IsVisible & !memberInfo[i].ReflectedType.IsSpecialName)
                    {
                        if (memberInfo[i].MemberType == MemberTypes.Method)
                        {
                            // filter out reflected property accessor methods
                            if (!memberInfo[i].Name.StartsWith("get_") && !memberInfo[i].Name.StartsWith("set_") && !memberInfo[i].Name.StartsWith("add_") && !memberInfo[i].Name.StartsWith("remove_") && !memberInfo[i].Name.StartsWith("op_"))
                            {
                                string methodParameters = "(";

                                ParameterInfo[] parameterInfo = ((MethodInfo)memberInfo[i]).GetParameters();
                                for (int p = 0; p < parameterInfo.Length; p++)
                                {
                                    methodParameters += parameterInfo[p].ParameterType.Name + " " + parameterInfo[p].Name;

                                    if (p < parameterInfo.Length - 1)
                                    {
                                        methodParameters += ", ";
                                    }
                                }

                                methodParameters += ")";

                                typeItems.Add(new IntellisenseListBoxItem(memberInfo[i].Name + methodParameters, memberInfo[i].Name + (parameterInfo.Length == 0 ? "()" : "("), 2));
                            }
                        }
                        else if (memberInfo[i].MemberType == MemberTypes.Property)
                        {
                            typeItems.Add(new IntellisenseListBoxItem(memberInfo[i].Name, memberInfo[i].Name, 3));
                        }
                        else if (memberInfo[i].MemberType == MemberTypes.Event)
                        {
                            typeItems.Add(new IntellisenseListBoxItem(memberInfo[i].Name, memberInfo[i].Name, 4));
                        }
                    }
                }

                typeItems.Sort();
                this.Items.AddRange(typeItems.ToArray());
            }

            protected override void OnDrawItem(System.Windows.Forms.DrawItemEventArgs e)
            {
                e.DrawBackground();
                e.DrawFocusRectangle();
                IntellisenseListBoxItem item;
                Rectangle bounds = e.Bounds;
                try
                {
                    Size imageSize = imageList.ImageSize;
                    item = (IntellisenseListBoxItem)Items[e.Index];
                    if (item.ImageIndex != -1)
                    {
                        imageList.Draw(e.Graphics, bounds.Left, bounds.Top, item.ImageIndex);
                        e.Graphics.DrawString(item.Text, e.Font, new SolidBrush(e.ForeColor),
                            bounds.Left + imageSize.Width + 2, bounds.Top);
                    }
                    else
                    {
                        e.Graphics.DrawString(item.Text, e.Font, new SolidBrush(e.ForeColor),
                            bounds.Left, bounds.Top);
                    }
                }
                catch
                {
                    if (e.Index != -1)
                    {
                        e.Graphics.DrawString(Items[e.Index].ToString(), e.Font,
                            new SolidBrush(e.ForeColor), bounds.Left, bounds.Top);
                    }
                    else
                    {
                        e.Graphics.DrawString(Text, e.Font, new SolidBrush(e.ForeColor),
                            bounds.Left, bounds.Top);
                    }
                }
                base.OnDrawItem(e);
            }
        }//End of GListBox class

        // GListBoxItem class 
        public class IntellisenseListBoxItem : IComparable
        {
            public int CompareTo(object obj)
            {
                if (obj != null)
                {
                    if (obj is IntellisenseListBoxItem)
                    {
                        IntellisenseListBoxItem item = (IntellisenseListBoxItem)obj;
                        return (this.Text.CompareTo(item.Text));
                    }
                    else
                    {
                        return 0;
                    }
                }

                return 0;
            }

            private string text;
            private string tag = string.Empty;

            public string Tag
            {
                get
                {
                    if (tag != string.Empty)
                    {
                        return tag;
                    }
                    else
                    {
                        return text;
                    }
                }
                set { tag = value; }
            }
            private int imageIndex;
            // properties 
            public string Text
            {
                get { return text; }
                set { text = value; }
            }
            public int ImageIndex
            {
                get { return imageIndex; }
                set { imageIndex = value; }
            }
            //constructor
            public IntellisenseListBoxItem(string text, int index)
            {
                this.text = text;
                imageIndex = index;
            }
            //constructor
            public IntellisenseListBoxItem(string text, string tag, int index)
            {
                this.tag = tag;
                this.text = text;
                imageIndex = index;
            }
            public IntellisenseListBoxItem(string text) : this(text, -1) { }
            public IntellisenseListBoxItem() : this("Unnamed item") { }
            public override string ToString()
            {
                return Tag;
            }
        }//End of GListBoxItem class

    

}
