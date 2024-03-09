using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace DbmlToClass
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            textBoxInputFile.Text = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "input", "sample.dbml");
            textBoxOutputFolder.Text = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "output");
        }

        private void buttonSelectInput_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Path.GetDirectoryName(textBoxInputFile.Text);
            openFileDialog1.FileName = Path.GetFileName(textBoxInputFile.Text);

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
                textBoxInputFile.Text = openFileDialog1.FileName;
        }

        private void buttonSelectOutput_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = textBoxOutputFolder.Text;

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                textBoxOutputFolder.Text = folderBrowserDialog1.SelectedPath;
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (!File.Exists(textBoxInputFile.Text))
            {
                MessageBox.Show("The specified input file could not be located.");
                return;
            }

            if (!Directory.Exists(textBoxOutputFolder.Text))
            {
                if (MessageBox.Show("The specified output folder could not be located. Do you want to create it?", "Action", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    try
                    {
                        Directory.CreateDirectory(textBoxOutputFolder.Text);
                    }
                    catch
                    {
                        MessageBox.Show("The folder could not be created, please check the path or use an existing folder.");
                        return;
                    }
                }
                else
                    return;
            }

            //read and parse the XML data
            string xml = File.ReadAllText(textBoxInputFile.Text);

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);

            XmlNode databaseNode = null;

            foreach (XmlNode xmlNode in xmlDocument.ChildNodes)
                if (xmlNode.Name == "Database")
                    databaseNode = xmlNode;

            foreach (XmlNode tableNode in databaseNode.ChildNodes)
            {
                if (tableNode.Name == "Table")
                {
                    XmlNode typeNode = tableNode.FirstChild;

                    if (typeNode != null && typeNode.Name == "Type")
                    {
                        string typeName = typeNode.Attributes["Name"].Value;

                        string classText = string.Empty;

                        classText += "using System;" + "\r\n";
                        classText += "" + "\r\n";
                        classText += "namespace " + txtNameSpace.Text + "\r\n";
                        classText += "{" + "\r\n";
                        classText += "    public class " + typeName + "\r\n";
                        classText += "    {" + "\r\n";

                        foreach (XmlNode columnNode in typeNode.ChildNodes)
                        {
                            if (columnNode.Name == "Column")
                            {
                                Type type = Type.GetType(columnNode.Attributes["Type"].Value);

                                // if type is null?
                                if (type == null)
                                    type = Type.GetType("System.String");

                                bool isNullable = bool.Parse(columnNode.Attributes["CanBeNull"].Value) && type.IsValueType;
                                string name = columnNode.Attributes["Name"].Value;

                                classText += "        public " + type.Name + (isNullable ? "?" : "") + " " + name + " { get; set; }" + "\r\n";
                            }
                        }

                        classText += "    }" + "\r\n";
                        classText += "}" + "\r\n";

                        string outputFile = Path.Combine(textBoxOutputFolder.Text, typeName + ".cs");
                        File.WriteAllText(outputFile, classText);
                    }
                }
            }

            MessageBox.Show("Done!");
            Process.Start(textBoxOutputFolder.Text);    
        }
    }
}
