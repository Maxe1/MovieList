using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Text;
using InterfacePlugin;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace Films_Library
{
    public partial class Form1 : Form
    {
        private string path = null;
        private OpenFileDialog opendialog;
        private readonly string pluginPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Plugins");
        private List<IPlugin> plugins = new List<IPlugin>();
        private List<OutCheckBox> filters = new List<OutCheckBox>();

        public Form1()
        {
            InitializeComponent();
            RefreshPlugins();
        }

        private void RefreshPlugins()
        {
            plugins.Clear();
            DirectoryInfo pluginDirectory = new DirectoryInfo(pluginPath);
            if (!pluginDirectory.Exists)
                pluginDirectory.Create();
            //берем из директории все файлы с расширением .dll      
            var pluginFiles = Directory.GetFiles(pluginPath, "*.dll");
            foreach (var file in pluginFiles)
            {
                //загружаем сборку
                Assembly asm = Assembly.LoadFrom(file);
                //ищем типы, имплементирующие наш интерфейс IPlugin,
                //чтобы не захватить лишнего
                var types = asm.GetTypes().
                                Where(t => t.GetInterfaces().
                                Where(i => i.FullName == typeof(IPlugin).FullName).Any());

                //заполняем экземплярами полученных типов коллекцию плагинов
                foreach (var type in types)
                {
                    var plugin = asm.CreateInstance(type.FullName) as IPlugin;
                    plugins.Add(plugin);
                }
            }
        }

        /// <summary>
        /// Output information from file to fields with plugins
        /// working by query and method SetTextList
        /// </summary>
        /// <param name="flag">set true or false if you have plugins or not</param>
        public void emptyPlugin(bool flag)
        {
            Form1 form = new Form1();
            form.listBox1.Items.Add(" ");
            XDocument xdoc = XDocument.Load(path);
            MovieList actor = new MovieList();

            var items = from xe in xdoc.Element("movies").Elements("movie")
                        select new
                        {
                            Title = xe.Element("title").Value,
                            Year = xe.Element("year").Value,
                            Country = xe.Element("country").Value,
                            Genre = xe.Element("genre").Value,
                            Director = String.Join(" ", xe.Element("director").Element("first_name").Value, xe.Element("director").Element("last_name").Value, xe.Element("director").Element("birth_date").Value),
                            Actor = String.Join(" ", xe.Element("actor").Element("first_name").Value, xe.Element("actor").Element("last_name").Value, xe.Element("actor").Element("birth_date").Value),

                            actor = from xe1 in xe.Elements("actor")
                                    select new MovieList
                                    {
                            First_name = xe1.Element("first_name").Value,
                            Last_name = xe1.Element("last_name").Value,
                            Birth_date = xe1.Element("birth_date").Value}
                        };


            if (flag == true)
            {
                foreach (var item in items)
                {
                    SetTextList(item.Title, item.Year, item.Country, item.Genre, item.Director, item.Actor);
                }
            }
            else
            {
                foreach (var item in items)
                {
                    listBox1.Items.Add(item.Title);
                }  
            }
        }
        /// <summary>
        /// Work with plugins and open file *.xml in OpnDialog method.
        /// </summary>
        public void openXmlToolStripMenuItem_Click(object sender, EventArgs e)
        {

            path = OpnDialog();
            if (path == null)
            {
                MessageBox.Show("file doesn't exist ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                opendialog.Reset();
            }
            else
            {
                List<string> nameFilm = new List<string>();
                bool flag = true;
                listBox1.Items.Clear();
                flowLayoutPanel1.Controls.Clear();
                if (plugins.Count == 0)
                {

                    try
                    {
                        emptyPlugin(flag);
                    }
                    catch (SecurityException ex)
                    {
                        MessageBox.Show("Security error.\n\nError message: {ex.Message}\n\n" +
                        "Details:\n\n{ex.StackTrace}" + ex);
                    }
                }
                else
                {
                    flag = false;
                    emptyPlugin(flag);
                    outplugins(path);
                }
            }
        }

        public class MovieList
        {
            public string First_name { get; set; }
            public string Last_name { get; set; }
            public string Birth_date { get; set; }
        }

        /// <summary>
        /// Method for output information on the list and text box.
        /// </summary>
        public void SetTextList(string title, string year,string country,string genre,string director,string actor) {
            textBox1.Text = textBox1.Text + ("Film: " + title + Environment.NewLine + "Year: " + year + Environment.NewLine
            + "Country: " + country + Environment.NewLine + "Genre: " + genre + Environment.NewLine + "Director: " + director
            + Environment.NewLine + "Actor: " + actor + Environment.NewLine);
            textBox1.Text = textBox1.Text + Environment.NewLine;
            listBox1.Items.Add(title);
        }
    
        public string RemoveSpace(string str) {
            var res = str.Replace(" ", "");
            return res;
        }
        /// <summary>
        /// One of main method of program are filtering information from movie file and show on box to depending on the filter.
        /// </summary>
        public void show()
        {
           XDocument xdoc = XDocument.Load(path);
           var value = xdoc.Element("movies").Elements("movie");
           var qwerty = from xe in xdoc.Element("movies").Elements("movie")
                       select new
                       {
                           Title = xe.Element("title").Value,
                           Year = xe.Element("year").Value,
                           Country = xe.Element("country").Value,
                           Genre = xe.Element("genre").Value,
                           Director = String.Join(" ", xe.Element("director").Element("first_name").Value, xe.Element("director").Element("last_name").Value, xe.Element("director").Element("birth_date").Value),
                           actor = from xe1 in xe.Elements("actor")
                                   select new MovieList
                                   {
                                       First_name = xe1.Element("first_name").Value,
                                       Last_name = xe1.Element("last_name").Value,
                                       Birth_date = xe1.Element("birth_date").Value
                                   }
                       };
            

           foreach (var index in this.filters) 
           {
               if (index.select.Count > 0)       
               {
                   if (index.name == "actor") {
                       List<string> films = new List<string>();
                       foreach (var aa in qwerty)
                       {
                           if (aa.actor.Any(k => index.select.Exists(y => y.Contains(k.First_name))))
                           {
                               films.Add(aa.Title);
                           }
                       }              
                       value.Where(x => !films.Exists(y => x.Value.Contains(y))).Remove();
                   }
                   else
                   {
                       value.Where(x => !(RemoveSpace(String.Join("", index.select))).Contains(x.Element(index.name).Value)).Remove();
                   }
               }
           }
           var items = from xe in value
                       select new Movie
                       {
                           Title = xe.Element("title").Value,
                           Year = xe.Element("year").Value,
                           Country = xe.Element("country").Value,
                           Genre = xe.Element("genre").Value,
                           Director = String.Join(" ", xe.Element("director").Element("first_name").Value, xe.Element("director").Element("last_name").Value, xe.Element("director").Element("birth_date").Value),
                           Actor = String.Join(" ", xe.Element("actor").Element("first_name").Value, xe.Element("actor").Element("last_name").Value, xe.Element("actor").Element("birth_date").Value),
                       };

            listBox1.Items.Clear();
            textBox1.Text = "";
                foreach (var item in items)
                {
                    SetTextList(item.Title, item.Year, item.Country , item.Genre,  item.Director,item.Actor );
                }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            listBox1.Items.Clear();

        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Films List App", "About...", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        /// <summary>
        /// The method draw our checkdox list on the form, depending onthe how many plugins consist of directory Plugins
        /// </summary>
        /// <param name="path">The file path for openDialog</param>
        private void outplugins(string path)
        {
            try
            {
                foreach (var plugin in plugins)
                {
                    var control = new OutCheckBox(plugin, path, this);
                    this.filters.Add(control);
                    flowLayoutPanel1.Controls.Add(control);
                }
            }

            catch (SecurityException ex)
            {
                MessageBox.Show("Security error.\n\nError message: {ex.Message}\n\n" +
                "Details:\n\n{ex.StackTrace}" + ex);
            }

        }
        

        private string OpnDialog()
        {
            opendialog = new OpenFileDialog();
            opendialog.Filter = "XML Files (*.xml)|*.xml";
            opendialog.FilterIndex = 1;
            opendialog.Multiselect = false;
            if (opendialog.ShowDialog() == DialogResult.OK)
            {
                path = opendialog.FileName;
            }
            else
            {
                DialogResult dialogResult = MessageBox.Show("Open file *.xml", "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                if (dialogResult == DialogResult.OK)
                {
                    OpnDialog();
                }
                if (dialogResult == DialogResult.Cancel)
                {
                    opendialog.Reset();

                }
            }

            return path;
        }

    }


    public class OutCheckBox : UserControl
    {
        public List<string> select = new List<string>();
        public string name;
        private Form1 form;

        public OutCheckBox(IPlugin plugin, string path, Form1 form)
        {
            this.form = form;
            InitializeComponent();
            object[] array = plugin.OutPut(path).Cast<Object>().ToArray();
            this.name = plugin.name();
            this.label1.Text = this.name.ToUpper() + ":";
            checkedListBox1.Items.AddRange(array);
            checkedListBox1.CheckOnClick = true;
        }

        void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e) //  
        {
            if (e.NewValue == CheckState.Checked)
                this.select.Add(checkedListBox1.Items[e.Index].ToString());
            else
                this.select.Remove(checkedListBox1.Items[e.Index].ToString());

            this.form.show();
        }

        private void InitializeComponent()
        {
            this.checkedListBox1 = new System.Windows.Forms.CheckedListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout(); 
            this.checkedListBox1.Location = new System.Drawing.Point(10, 20);
            this.checkedListBox1.Size = new System.Drawing.Size(140, 125);
            this.checkedListBox1.ItemCheck += new ItemCheckEventHandler(this.checkedListBox1_ItemCheck);
            this.checkedListBox1.TabIndex = 0;
            this.label1.Location = new System.Drawing.Point(10, 5);
            this.Controls.Add(this.checkedListBox1);
            this.Controls.Add(this.label1);
            this.Size = new System.Drawing.Size(150, 145);
            this.ResumeLayout(false);
        }
        private System.Windows.Forms.CheckedListBox checkedListBox1;
        private System.Windows.Forms.Label label1;

    }

    public class Filter
    {
        public string name;
        public string[] values;
    }

    public class Movie
    {
        public string Title { get; set; }
        public string Year { get; set; }
        public string Country { get; set; }
        public string Genre { get; set; }
        public string Director { get; set; }
        public string Actor { get; set; }
    }

    
}
