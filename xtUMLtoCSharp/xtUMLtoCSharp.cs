﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using Newtonsoft.Json;
using UMLtoSourceCode.Class;
using static System.Windows.Forms.AxHost;
using static UMLtoSourceCode.Class.JsonData;
using Newtonsoft.Json.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections;
using System.Net.NetworkInformation;

namespace UMLtoSourceCode
{
    public partial class xtUMLtoCSharp : Form
    {
        public string JSONFile;
        public string dataType;

        StringBuilder SourceCodeBuilder = new StringBuilder();
        StringBuilder AssocBuilder = new StringBuilder();

        public xtUMLtoCSharp()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        public void reset()
        {
            textBox1.Clear();
            textBox2.Clear();
            textBox3.Clear();
            JSONFile = null;
        }
        

        public void converterJSONtoCSharp(string umlDiagramJson)
        {
            JsonData json = JsonConvert.DeserializeObject<JsonData>(umlDiagramJson);

            SourceCodeBuilder.Clear();
            AssocBuilder.Clear();

            // Assoc Builder START
            StringBuilder aB = new StringBuilder();
            foreach (JsonData.Model model in json.model.Where(model => model.type == "association"))
            {
                if (model.model != null)
                {
                    foreach (JsonData.Class1 class1 in model.@class)
                    {
                        AssocBuilder.AppendLine($"      " +
                        $"{class1.class_name} public List<{model.model.class_name}> {model.model.class_name}List  " + "{ get; set; }");
                    }
                }
                if (model.model == null)
                {
                    foreach (JsonData.Class1 class1 in model.@class)
                    {
                        aB.Append($"{class1.class_name} {class1.class_multiplicity}" + " ");
                    }
                    string aB_ = aB.ToString();
                    string[] words = aB_.Split();

                    if (words.Length >= 4)
                    {
                        string fstC = words[0];
                        string fstM = words[1];
                        string sndC = words[2];
                        string sndM = words[3];
                        string assoc = null;
                        string assoc1 = null;
                        if (fstM == "1..*" || fstM == "0..*")
                        {
                            assoc = $"{fstC} public List<{sndC}> {sndC}List " + "{ get; set; }";
                        } 
                        else if (fstM == "1..1")
                        {
                            assoc = $"{fstC} public {sndC} {sndC} " + "{ get; set; }";
                        }

                        if (sndM == "1..*" || sndM == "0..*")
                        {
                            assoc1 = $"{sndC} public List<{fstC}> {fstC} " + "{ get; set; }";
                        }
                        else if (sndM == "1..1")
                        {
                            assoc1 = $"{sndC} public {fstC} {fstC} " + "{ get; set; }";
                        }

                        AssocBuilder.AppendLine(assoc);
                        AssocBuilder.AppendLine(assoc1);
                    }
                    aB.Clear();
                }
            }
            // Assoc Builder END
            SourceCodeBuilder.AppendLine("");
            SourceCodeBuilder.AppendLine($"// {json.sub_name}");
            SourceCodeBuilder.AppendLine($"namespace {json.sub_name}");
            SourceCodeBuilder.AppendLine("{");

            // STATES START
            foreach (JsonData.Model model in json.model)
            {
                var states = new List<string>();
                if (model.states != null)
                {
                    foreach (JsonData.State state in model.states)
                    {
                        string stateAdd = state.state_name.Replace(" ", "");
                        states.Add(stateAdd);
                    }
                    SourceCodeBuilder.AppendLine("   " +
                        $"public enum {model.class_name}States" + "\n   {");
                    foreach (var state in states)
                    {
                        SourceCodeBuilder.AppendLine("      " +
                            $"{state},");
                    }
                    SourceCodeBuilder.AppendLine("   }");
                }
            }
            SourceCodeBuilder.AppendLine("");
            // STATES END

            SourceCodeBuilder.AppendLine("   public class Timer");
            SourceCodeBuilder.AppendLine("   {");
            SourceCodeBuilder.AppendLine("      public void Start() { }");
            SourceCodeBuilder.AppendLine("      public void Stop() { }");
            SourceCodeBuilder.AppendLine("   }");

            // Classes START
            foreach (JsonData.Model model in json.model)
            {
                SourceCodeBuilder.AppendLine("");
                if (model.type == "class" || model.type == "imported_class")
                {
                    var attrInfoList = new List<string>();

                    string stateAttribute = null;

                    SourceCodeBuilder.AppendLine($"   public class {model.class_name}");
                    SourceCodeBuilder.AppendLine("   {");

                    foreach (JsonData.Attribute1 attr in model.attributes)
                    {
                        string dataType = attr.data_type;
                        if (dataType == "id")
                        {
                            dataType = "char";
                        }
                        else if (dataType == "integer")
                        {
                            dataType = "int";
                        }
                        else if (dataType == "real")
                        {
                            dataType = "double";
                        }

                        if (attr.default_value != null)
                        {
                            stateAttribute = attr.attribute_name;
                            string input = attr.default_value;
                            int dot = input.IndexOf('.');
                            if (dot != -1)
                            {
                                string state = input.Substring(dot + 1);
                                SourceCodeBuilder.AppendLine("      " +
                                    $"public {model.class_name}States {attr.attribute_name} " + "{ get; set; }" + $" = {model.class_name}States.{state}" + ";");
                            }
                            else
                            {
                                {
                                    SourceCodeBuilder.AppendLine("      " +
                                        $"public {model.class_name}States {attr.attribute_name} " + "{ get; set; }" + $" = {model.class_name}States.{input}" + ";");
                                }
                            }

                        }
                        else if (attr.data_type == "id")
                        {
                            if (attr.attribute_type == "referential_attribute")
                            {
                                SourceCodeBuilder.AppendLine("      " +
                                    $"public {dataType} {attr.attribute_name} " + "{ get; set; } // Referential Attribute");
                            }
                            else
                            {
                                SourceCodeBuilder.AppendLine("      " +
                                $"public {dataType} {attr.attribute_name} " + "{ get; set; }");
                            }
                        }
                        else if (attr.data_type == "inst_event")
                        {
                            SourceCodeBuilder.AppendLine("");
                            string cName = null;
                            foreach (JsonData.Model modell in json.model)
                            {
                                if (modell.class_id == attr.class_id)
                                {
                                    cName = modell.class_name;
                                }
                            }
                            SourceCodeBuilder.AppendLine("      " +
                                $"public void {attr.event_name}({cName} {cName})");
                            SourceCodeBuilder.AppendLine("      " +
                                "{");
                            SourceCodeBuilder.AppendLine("         " +
                                $"{cName}.status = {cName}States.{attr.state_name};");
                            SourceCodeBuilder.AppendLine("      " +
                                "}");
                            SourceCodeBuilder.AppendLine("");
                        }
                        else if (attr.data_type == "inst_ref")
                        {
                            SourceCodeBuilder.AppendLine("      " +
                                $"public {attr.related_class_name} {attr.attribute_name}Ref" + " { get; set; }");
                        }
                        else if (attr.data_type == "inst_ref_set")
                        {
                            SourceCodeBuilder.AppendLine("      " +
                                $"public {attr.related_class_name}[] {attr.attribute_name}RefSet" + " { get; set; }");
                        }
                        else if (attr.data_type == "inst_ref_<timer>")
                        {
                            SourceCodeBuilder.AppendLine("      " +
                                $"public {attr.related_class_name} {attr.attribute_name}" + " { get; set; }");
                        }
                        else
                        {
                            SourceCodeBuilder.AppendLine("      " +
                                $"public {dataType} {attr.attribute_name} " + "{ get; set; }");
                        }

                        if (attr.data_type != "state" && attr.data_type != "inst_event" && attr.data_type != "inst_ref" && attr.data_type != "inst_ref_set" && attr.data_type != "inst_ref_<timer>")
                        {
                            if (attr.attribute_type != "referential_attribute")
                            {
                                string attrInfo = $"{dataType} {attr.attribute_name}";
                                attrInfoList.Add(attrInfo);
                            }
                        }
                    }

                    // Associations START
                    foreach (var assoc in AssocBuilder.ToString().Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string assocTrim = assoc.Trim();
                        int stSpace = assocTrim.IndexOf(' ');

                        if (assocTrim.StartsWith($"{model.class_name} ", StringComparison.OrdinalIgnoreCase))
                        {
                            SourceCodeBuilder.AppendLine("      " +
                                assocTrim.Substring(stSpace + 1));
                        }
                    }
                    // Associations END

                    // STATES EVENT AND FUNCTIONS START
                    if (model.states != null)
                    {
                        SourceCodeBuilder.AppendLine("");
                        SourceCodeBuilder.AppendLine("      " +
                                        $"public void onStateAction()");
                        SourceCodeBuilder.AppendLine("      {");
                        SourceCodeBuilder.AppendLine("           " +
                            $"switch({stateAttribute})");
                        SourceCodeBuilder.AppendLine("           {");
                        foreach (JsonData.State statess in model.states)
                        {
                            SourceCodeBuilder.AppendLine("              " +
                                $"case {model.class_name}States.{statess.state_name.Replace(" ", "")}:");
                            SourceCodeBuilder.AppendLine("                  " +
                                "// implementations code here");
                            if (statess.transitions != null)
                            {
                                foreach (var transition in statess.transitions)
                                {
                                    string targetState = null;
                                    foreach (JsonData.State states in model.states)
                                    {
                                        if (states.state_id == transition.target_state_id)
                                        {
                                            targetState = states.state_event.ToString();
                                        }
                                    }
                                    SourceCodeBuilder.AppendLine("                  " +
                                        $"if ({stateAttribute} == {model.class_name}States.{transition.target_state.Replace(" ", "")})");
                                    SourceCodeBuilder.AppendLine("                  {");
                                    SourceCodeBuilder.AppendLine("                      " +
                                        $"{targetState}();");
                                    SourceCodeBuilder.AppendLine("                  }");
                                }
                            }
                            SourceCodeBuilder.AppendLine("                  " +
                                "break;");
                        }
                        SourceCodeBuilder.AppendLine("              " +
                                $"default:");
                        SourceCodeBuilder.AppendLine("                  " +
                                "break;");
                        SourceCodeBuilder.AppendLine("           }");
                        SourceCodeBuilder.AppendLine("      }");
                        foreach (JsonData.State state in model.states)
                        {
                            if (state.state_function != null)
                            {
                                foreach (var stateFunction in state.state_function)
                                {
                                    SourceCodeBuilder.AppendLine("");
                                    SourceCodeBuilder.AppendLine("      " +  
                                        $"public void {stateFunction}()");
                                    SourceCodeBuilder.AppendLine("      {");
                                    foreach (JsonData.Attribute1 attr in model.attributes)
                                    {
                                        if (attr.data_type == "state")
                                        {
                                            SourceCodeBuilder.AppendLine("           " +
                                                    $"if ({attr.attribute_name} != {model.class_name}States.{state.state_name.Replace(" ", "")})");
                                            SourceCodeBuilder.AppendLine("           {");
                                            SourceCodeBuilder.AppendLine("               " +
                                                $"{attr.attribute_name} = {model.class_name}States.{state.state_name.Replace(" ", "")};");
                                            SourceCodeBuilder.AppendLine("           }");
                                        }
                                    }
                                    SourceCodeBuilder.AppendLine("      }");
                                }
                            }

                            void stateEventBuilder(string stateEvent)
                            {
                                SourceCodeBuilder.AppendLine("");
                                SourceCodeBuilder.AppendLine("      " +
                                        $"public void {stateEvent}()");
                                SourceCodeBuilder.AppendLine("      {");

                                foreach (JsonData.Attribute1 attr in model.attributes)
                                {
                                    if (attr.data_type == "state")
                                    {
                                        SourceCodeBuilder.AppendLine("           " +
                                                $"if ({attr.attribute_name} != {model.class_name}States.{state.state_name.Replace(" ", "")})");
                                        SourceCodeBuilder.AppendLine("           {");
                                        SourceCodeBuilder.AppendLine("               " +
                                            $"{attr.attribute_name} = {model.class_name}States.{state.state_name.Replace(" ", "")};");
                                        SourceCodeBuilder.AppendLine("           }");

                                    }
                                }
                                SourceCodeBuilder.AppendLine("      }");
                            }

                            if (state.state_event != null)
                            {
                                var stateEventArray = state.state_event as JArray;
                                if (stateEventArray != null)
                                {
                                    foreach (var item in stateEventArray)
                                    {
                                        string stateEvent = item.ToString();
                                        if (!stateEvent.StartsWith("on", StringComparison.OrdinalIgnoreCase))
                                        {
                                            stateEventBuilder(stateEvent);
                                        }
                                    }
                                }
                                else if (state.state_event is string)
                                {
                                    string stateEvent = state.state_event.ToString();
                                    stateEventBuilder(stateEvent);
                                }
                            }
                        }
                    }
                    // STATES EVENT AND FUNCTIONS END

                    SourceCodeBuilder.AppendLine("");

                    string constructor = string.Join(", ", attrInfoList);
                    SourceCodeBuilder.AppendLine("      " +
                        $"public {model.class_name} ({constructor})");
                    SourceCodeBuilder.AppendLine("       {");
                    foreach (JsonData.Attribute1 attr in model.attributes)
                    {
                        if (attr.data_type != "state" 
                            && attr.data_type != "inst_event" 
                            && attr.data_type != "inst_ref" 
                            && attr.data_type != "inst_ref_set"
                            && attr.attribute_type != "referential_attribute")
                        {
                            if (attr.data_type == "inst_ref_<timer>")
                            {
                                SourceCodeBuilder.AppendLine("           " +
                                    $"{attr.attribute_name} = new {attr.related_class_name}();");
                            }
                            else
                            {
                                SourceCodeBuilder.AppendLine("           " +
                                $"this.{attr.attribute_name} = {attr.attribute_name};");
                            }
                        }
                    }
                    SourceCodeBuilder.AppendLine("       }");
                    SourceCodeBuilder.AppendLine("   }");
                    SourceCodeBuilder.AppendLine("");
                }
                // Classes END

                // Associations1 START
                if (model.type == "association" && model.model != null)
                {
                    SourceCodeBuilder.AppendLine("   " +
                        $"public class {model.model.class_name}");
                    SourceCodeBuilder.AppendLine("   {");

                    foreach (JsonData.Attribute assocAttr in model.model.attributes)
                    {
                        string dataType = assocAttr.data_type;
                        if ((dataType == "id") || (dataType == "integer"))
                        {
                            dataType = "int";
                        }
                        else if (dataType == "real")
                        {
                            dataType = "double";
                        }
                        if (assocAttr.attribute_type == "referential_attribute")
                        {
                            SourceCodeBuilder.AppendLine("      " +
                                $"public {dataType} {assocAttr.attribute_name} " + "{ get; set; } // Referential Attribute");
                        }
                        else
                        {
                            SourceCodeBuilder.AppendLine("      " +
                                $"public {dataType} {assocAttr.attribute_name} " + "{ get; set; }");
                        }
                    }
                    SourceCodeBuilder.AppendLine("");
                    foreach (JsonData.Class1 asoc_class in model.@class)
                    {
                        if (asoc_class.class_multiplicity == "1..1")
                        {
                            SourceCodeBuilder.AppendLine("      " +
                                $"public {asoc_class.class_name} {asoc_class.class_name} " + "{ get; set; }");
                        }
                        else
                        {
                            SourceCodeBuilder.AppendLine("      " +
                                $"public List<{asoc_class.class_name}> {asoc_class.class_name}List " + "{ get; set; }");
                        }
                    }
                    SourceCodeBuilder.AppendLine("   }");
                    SourceCodeBuilder.AppendLine("");
                }
            }
            // Associations1 END

            SourceCodeBuilder.AppendLine("  class Program");
            SourceCodeBuilder.AppendLine("  {");
            SourceCodeBuilder.AppendLine("      static void Main(string[] args)");
            SourceCodeBuilder.AppendLine("      {");
            SourceCodeBuilder.AppendLine("          // Write your code here");
            SourceCodeBuilder.AppendLine("      }");
            SourceCodeBuilder.AppendLine("  }");

            SourceCodeBuilder.AppendLine("}");

            string SourceCode = SourceCodeBuilder.ToString();
            textBox3.AppendText(SourceCode);
        }

        public void JSONtoCSharp(string inputFile)
        {
            if (inputFile == null)
            {
                MessageBox.Show("No Json File selected!!");
                return;
            }
            string umlDiagramJson = File.ReadAllText(inputFile);

            label1.Text = "";
            textBox3.Clear();

            converterJSONtoCSharp(umlDiagramJson);
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Open Json Diagram File";
            dialog.Filter = "Json Diagram Files|*.json";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                JSONFile = dialog.FileName;
                string displayJson = File.ReadAllText(JSONFile);
                textBox1.Text = JSONFile;
                textBox2.Text = displayJson;
            }
        }
        
        private void btnTranslate_Click(object sender, EventArgs e)
        {
                JSONtoCSharp(JSONFile);
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textBox3.Text);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            reset();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();

            dialog.Title = "Save into C# Source code";
            dialog.DefaultExt = "cs";
            dialog.Filter = "C# Source code (*.cs)|*.cs|C# Source code (*.*)|*.*";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string fileName = dialog.FileName;

                File.WriteAllText(fileName, textBox3.Text);
            }
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("1. Press 'Select File' button to select .json file to convert \n" +
                "\n" +
                "2. Press 'Translate' button to convert your selected json file into c# source code \n" +
                "\n" +
                "3. Output will be displayed on richTextBox \n" +
                "\n" +
                "4. Press 'Save' button to save the output into a file \n" +
                "\n" +
                "5. Press 'Reset' button to reset input, output, and selected file" +
                "\n", "User Guide", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
