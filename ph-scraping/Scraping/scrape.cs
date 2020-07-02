// Created by Patrick Sherbondy
// 4/27/2020: Implemented ability to get all relevant information from the Diagnoses files.

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DiagnosesScraper
{
    class Symptom
    {
        public string SymptomName { get; set; }
        public string Probability { get; set; }
        public List<string> Examinations { get; set; }
        public string Treatment { get; set; }
        public string Collapse { get; set; }
        public string CollapseTime { get; set; }
        public string DeathTime { get; set; }

        public Symptom()
        {
            SymptomName = " ";
            Probability = " ";
            Examinations = new List<string>();
            Treatment = "None";
            Collapse = "N/A";
            CollapseTime = "N/A";
            DeathTime = "N/A";
        }

        public Symptom(string symptom, string probability, List<string> examinations, string treatment, string collapse, string collapseTime, string deathTime)
        {
            SymptomName = symptom;
            Probability = probability;
            Examinations = examinations;
            Treatment = treatment;
            Collapse = collapse;
            CollapseTime = collapseTime;
            DeathTime = deathTime;
        }

        public Symptom clone()
        {
            return (Symptom)this.MemberwiseClone();
        }
    }

    class Disease
    {
        public string DiseaseName { get; set; }
        public string Description { get; set; }
        public string Department { get; set; }
        public string Occurrence { get; set; }
        public string Payment { get; set; }
        public List<Symptom> Symptoms { get; set; }

        public Disease()
        {
            DiseaseName = " ";
            Description = " ";
            Department = " ";
            Occurrence = " ";
            Payment = " ";
            Symptoms = new List<Symptom>();
        }

        public Disease(string disease, string description, string department, string occurrence, string payment, List<Symptom> symptoms)
        {
            DiseaseName = disease;
            Description = description;
            Department = department;
            Occurrence = occurrence;
            Payment = payment;
            Symptoms = symptoms;
        }
    }

    class DeptData
    {
        public string DeptName { get; set; }
        public int TotalDiseases { get; set; }
        public List<string> ExamData { get; set; }

        public DeptData(string name)
        {
            DeptName = name;
            TotalDiseases = 0;
            ExamData = new List<string>();
        }
    }

    class Scrape
    {
        // Keeps a running list (yes I know it's a hashtable) of all possible symptoms in the game and their various stats.
        public static Hashtable symptomList = new Hashtable();
        // Keeps a running list of all possible diseases in the game and their various stats, including a list of symptoms.
        public static List<Disease> diseaseList = new List<Disease>();

        // Holds only unique strings for data-collection purposes.
        public static List<String> symptomsDataList = new List<String>();
        public static List<String> examsDataList = new List<String>();

        // Used for proper names and descriptions found in-game.
        public static Dictionary<string, string> diagnosesTable = new Dictionary<string, string>();
        public static Dictionary<string, string> examTable = new Dictionary<string, string>();
        public static Dictionary<string, string> symptomTable = new Dictionary<string, string>();
        public static Dictionary<string, string> treatmentTable = new Dictionary<string, string>();

        // This flag enables data collection for symptoms and exams. It will drag down runtime by a few uncomfortable seconds if set to true.
        // It is currently a little bit broken (it repeats some outputs) and vestigial.
        public static bool getDeptData = true;
        
        static void scrape()
        {
            string[] er = System.IO.File.ReadAllLines(@".\Diagnoses\DiagnosesER.txt", Encoding.UTF8);
            string[] surg = System.IO.File.ReadAllLines(@".\Diagnoses\DiagnosesSURG.txt", Encoding.UTF8);
            string[] intern = System.IO.File.ReadAllLines(@".\Diagnoses\DiagnosesINTERN.txt", Encoding.UTF8);
            string[] ortho = System.IO.File.ReadAllLines(@".\Diagnoses\DiagnosesORTHO.txt", Encoding.UTF8);
            string[] cardio = System.IO.File.ReadAllLines(@".\Diagnoses\DiagnosesCARDIO.txt", Encoding.UTF8);
            string[] neuro = System.IO.File.ReadAllLines(@".\Diagnoses\DiagnosesNEURO.txt", Encoding.UTF8);

            string[] symptoms1 = System.IO.File.ReadAllLines(@".\Symptoms\Symptoms_main.txt", Encoding.UTF8);
            string[] symptoms2 = System.IO.File.ReadAllLines(@".\Symptoms\Symptoms.txt", Encoding.UTF8);

            // Used to gather data on the various departments.
            DeptData erData = new DeptData("Emergency");
            DeptData surgData = new DeptData("General Surgery");
            DeptData internData = new DeptData("Internal Medicine");
            DeptData orthoData = new DeptData("Orthopedics");
            DeptData cardioData = new DeptData("Cardiology");
            DeptData neuroData = new DeptData("Neurology");
            
            // Processes the four files in the Localization folder, allowing for the proper in-game names and descriptions
            // to appear in the final output.
            processTable("DiagnosesTable.txt", diagnosesTable, true);
            processTable("ExamTable.txt", examTable, false);
            processTable("SymptomTable.txt", symptomTable, false);
            processTable("TreatmentTable.txt", treatmentTable, false);

            // Processes all possible symptoms in the game so that they can be paired with various diagnoses.
            processSymptoms(symptoms1);
            processSymptoms(symptoms2);

            // Processes the individual diseases for each department, bringing all the hard work from the previous
            // methods together.
            processDiagnoses(er, "emergency", erData);
            generateDataFile(erData);

            processDiagnoses(surg, "general-surgery", surgData);
            generateDataFile(surgData);

            processDiagnoses(intern, "internal-medicine", internData);
            generateDataFile(internData);

            processDiagnoses(ortho, "orthopedics", orthoData);
            generateDataFile(orthoData);

            processDiagnoses(cardio, "cardiology", cardioData);
            generateDataFile(cardioData);

            processDiagnoses(neuro, "neurology", neuroData);
            generateDataFile(neuroData);


            // Shockingly, this generates the output file.
            generateOutputFile();
        }

        static void generateDataFile(DeptData data)
        {
            if (!getDeptData)
            {
                return;
            }

            // Contains all treatments and symptoms across every currenttly-implemented disease. These arrays contain duplicate strings.
            List<string> allSymptoms = new List<string>();
            List<string> allExams = new List<string>();

            Dictionary<string, int> symptomFreq = new Dictionary<string, int>();
            Dictionary<string, int> examFreq = new Dictionary<string, int>();

            string file = @".\Output\" + data.DeptName.ToLower() + "-department-data.txt";

            if (File.Exists(file))
            {
                File.Delete(file);
            }

            FileStream ofs = File.Create(file);
            StreamWriter ofp = new StreamWriter(ofs);

            // Populate the string arrays.
            for (int i = 0; i < diseaseList.Count; i++)
            {
                for (int j = 0; j < diseaseList[i].Symptoms.Count; j++)
                {
                    allSymptoms.Add(diseaseList[i].Symptoms[j].SymptomName);

                    for (int k = 0; k < diseaseList[i].Symptoms[j].Examinations.Count; k++)
                    {
                        allExams.Add(diseaseList[i].Symptoms[j].Examinations[k]);
                    }
                }
            }

            ofp.WriteLine("{0} Data:\n", data.DeptName);

            ofp.WriteLine("Total Diagnoses: {0}", data.TotalDiseases);

            // ofp.WriteLine("Symptom Frequency:\n");

            // // Search for each individual string and start keeping track.
            // // Yes, I know it's horribly inefficient, but that's fine by me. Runtime is not a concern for this program.
            // for (int i = 0; i < allSymptoms.Count; i++)
            // {
            //     for (int j = 0; j < symptomsDataList.Count; j++)
            //     {
            //         int freq = search(allSymptoms, symptomsDataList[j]);
            //         //symptomFreq.Add(symptomsDataList[j], freq);
            //         ofp.WriteLine("{0} : {1}", symptomsDataList[j], freq);
            //     }
            // }

            ofp.WriteLine("\nExamination Frequency:\n");

            for (int i = 0; i < examsDataList.Count; i++)
            {
                for (int j = 0; j < examsDataList.Count; j++)
                {
                    int freq = search(allExams, examsDataList[j]);
                    //examFreq.Add(examsDataList[j], freq);

                    if (freq >= data.TotalDiseases / 4)
                    ofp.WriteLine("{0} : {1}", examsDataList[j], freq);
                }
            }

            // Wipe the lists for use in the next department.
            symptomsDataList.Clear();
            examsDataList.Clear();
            
            ofp.Flush();
            ofp.Close();
        }

        static int search(List<string> list, string target)
        {
            int count = 0;

            for (int i = 0; i < list.Count; i++)
            {
                if (target.Equals(list[i]))
                {
                    count++;
                }
            }

            return count;
        }

        static void generateOutputFile()
        {
            string file = @".\Output\formatted-diagnoses.txt";
            bool useSteamFormat = false, usePlainFormat = false;

            Console.WriteLine("Steam or Plain format? (S / P)");
            string input = Console.ReadLine();
            char inputChar = input[0];

            if (Char.ToLower(inputChar) == 's')
            {
                useSteamFormat = true;
            }
            else if (Char.ToLower(inputChar) == 'p')
            {
                usePlainFormat = true;
            }
            else
            {
                Console.WriteLine("Invalid input. Choosing default: Steam format.");
                useSteamFormat = true;
            }

            if (File.Exists(file))
            {
                File.Delete(file);
            }

            FileStream ofs = File.Create(file);
            StreamWriter ofp = new StreamWriter(ofs);

            //diseaseList.Sort((x, y) => x.DiseaseName.CompareTo(y.DiseaseName));

            if (useSteamFormat)
            {
                for (int i = 0; i < diseaseList.Count; i++)
                {
                    string temp = diseaseList[i].DiseaseName;
                    TextInfo info = new CultureInfo("en-US", false).TextInfo;

                    ofp.WriteLine("[u][b][h1]{0}[/h1][/b][/u]", info.ToTitleCase(temp));
                    ofp.WriteLine("[quote]{0}[/quote]", diseaseList[i].Description);
                    
                    ofp.WriteLine("");

                    ofp.WriteLine("Department: {0}", diseaseList[i].Department);
                    ofp.WriteLine("Occurrence: {0}", diseaseList[i].Occurrence);
                    ofp.WriteLine("Base Payment: {0}", diseaseList[i].Payment);

                    ofp.WriteLine("");

                    ofp.WriteLine("[h3]Symptoms[/h3]");
                    ofp.WriteLine("[table][tr][th]Symptom[/th][th]Probability[/th][th]Examination[/th][th]Treatment[/th][th]Collapse Symptom[/th][th]Collapse Risk Hours[/th][th]Death Risk Hours[/th][/tr]");

                    // Loop through each symptom for the current disease.
                    for (int j = 0; j < diseaseList[i].Symptoms.Count; j++)
                    {
                        ofp.WriteLine("[tr][td]{0}[/td][td]{1}[/td][td]{2}[/td][td]{3}[/td][td]{4}[/td][td]{5}[/td][td]{6}[/td][/tr]",
                        diseaseList[i].Symptoms[j].SymptomName, diseaseList[i].Symptoms[j].Probability, iterateExams(i, j),
                        diseaseList[i].Symptoms[j].Treatment, diseaseList[i].Symptoms[j].Collapse, diseaseList[i].Symptoms[j].CollapseTime,
                        diseaseList[i].Symptoms[j].DeathTime);
                    }

                    ofp.WriteLine("[/table]");
                    ofp.WriteLine("");
                }
            }
            else if (usePlainFormat)
            {
                for (int i = 0; i < diseaseList.Count; i++)
                {
                    string temp = diseaseList[i].DiseaseName;
                    TextInfo info = new CultureInfo("en-US", false).TextInfo;

                    ofp.WriteLine("{0}", info.ToTitleCase(temp));
                    ofp.WriteLine("{0}", diseaseList[i].Description);
                    
                    ofp.WriteLine("");

                    ofp.WriteLine("Department: {0}", diseaseList[i].Department);
                    ofp.WriteLine("Occurrence: {0}", diseaseList[i].Occurrence);
                    ofp.WriteLine("Base Payment: {0}", diseaseList[i].Payment);

                    ofp.WriteLine("");

                    ofp.WriteLine("Symptoms:");

                    // Loop through each symptom for the current disease.
                    for (int j = 0; j < diseaseList[i].Symptoms.Count; j++)
                    {
                        ofp.WriteLine("{0} - {1} - {2} - {3} - {4} - {5} - {6}",
                        diseaseList[i].Symptoms[j].SymptomName, diseaseList[i].Symptoms[j].Probability, iterateExams(i, j),
                        diseaseList[i].Symptoms[j].Treatment, diseaseList[i].Symptoms[j].Collapse, diseaseList[i].Symptoms[j].CollapseTime,
                        diseaseList[i].Symptoms[j].DeathTime);
                    }

                    ofp.WriteLine("");
                }
            }
            // Loop through every disease.
            

            ofp.Flush();
            ofp.Close();
        }

        // An auxilliary method used to produce desired output for the generateOutputFile() method.
        static string iterateExams(int i, int j)
        {
            string retStr = "";
            int k = 0;

            if (diseaseList[i].Symptoms[j].Examinations.Count <= 0)
            {
                return "None";
            }

            retStr = diseaseList[i].Symptoms[j].Examinations[k++];
            
            while (k < diseaseList[i].Symptoms[j].Examinations.Count)
            {
                retStr += ", " + diseaseList[i].Symptoms[j].Examinations[k];
                k++;
            }

            return retStr;
        }

        // Given an input file and hashtable, process the contents of the file and hash them
        // with their given itemIDs for future use.
        static void processTable(string filename, Dictionary<string, string> refTable, bool wantDescription)
        {
            string[] file = System.IO.File.ReadAllLines(@".\Localization\" + filename);

            for (int i = 0; i < file.Length; i++)
            {
                // Add the name of entry being processed.
                if (!file[i].Contains("DESCRIPTION") && file[i].Contains("<LocID>"))
                {
                    int beginText = file[i].IndexOf("<Text>") + "<Text>".Length;
                    int endText = file[i].IndexOf("</Text>");

                    int beginLoc = file[i].IndexOf("<LocID>") + "<LocID>".Length;
                    int endLoc = file[i].IndexOf("</LocID>");

                    string name = file[i].Substring(beginText, endText - beginText);
                    string nameID = file[i].Substring(beginLoc, endLoc - beginLoc);

                    refTable.Add(nameID, name);
                }
                // Add the description of the entry being processed.
                else if (file[i].Contains("DESCRIPTION") && wantDescription)
                {
                    int beginText = file[i].IndexOf("<Text>") + "<Text>".Length;
                    int endText = file[i].IndexOf("</Text>");

                    int beginLoc = file[i].IndexOf("<LocID>") + "<LocID>".Length;
                    int endLoc = file[i].IndexOf("</LocID>");

                    string description = file[i].Substring(beginText, endText - beginText);
                    string descriptionID = file[i].Substring(beginLoc, endLoc - beginLoc);

                    refTable.Add(descriptionID, description);
                }
            }
        }

        static void processSymptoms(string[] symptoms)
        {
            string collapseStart = "", collapseEnd = "";
            string deathStart = "", deathEnd = "";
            string nameID = "";

            bool foundCollapseStart = false, foundCollapseEnd = false;
            bool foundDeathStart = false, foundDeathEnd = false;

            // Loop through the symptoms file and add each symtom to the hashtable.
            for (int i = 0; i < symptoms.Length; i++)
            {
                Symptom symptom = new Symptom();
                string current = "current";

                // Locate the end of the symptom entry.
                while (!symptoms[i].Contains("</GameDBSymptom>"))
                {
                    // Extract the name of the symptom. This will be used to identify it for the final product.
                    if (symptoms[i].Contains("<GameDBSymptom ID="))
                    {
                        int start = symptoms[i].IndexOf("<GameDBSymptom ID=\"") + "<GameDBSymptom ID=\"".Length;
                        int end = symptoms[i].IndexOf("\">");
                        nameID = symptoms[i].Substring(start, end - start);

                        symptom.SymptomName = (string)symptomTable[nameID];
                    }
                    // Add all relevant examinations for this symptom.
                    else if (symptoms[i].Contains("<Examinations>"))
                    {
                        // Continually add all valid examinations for this symptom.
                        while (!symptoms[i].Contains("</Examinations>"))
                        {
                            int start = symptoms[i].IndexOf("<ExaminationRef>") + "<ExaminationRef>".Length;
                            int end = symptoms[i].IndexOf("</ExaminationRef>");

                            if (start >= 0 && end >= 0)
                            {
                                string examID = symptoms[i].Substring(start, end - start);
                                string exam = (string)examTable[examID];
                            
                                symptom.Examinations.Add(exam);
                            }

                            i++;
                        }
                    }
                    // Add the valid treatment of this symptom, if there is one.
                    else if (symptoms[i].Contains("<TreatmentRef>"))
                    {
                        int start = symptoms[i].IndexOf("<TreatmentRef>") + "<TreatmentRef>".Length;
                        int end = symptoms[i].IndexOf("</TreatmentRef>");

                        string treatmentID = symptoms[i].Substring(start, end - start);
                        string treatment = (string)treatmentTable[treatmentID];

                        symptom.Treatment = treatment;
                    }
                    // Adds the collapse symtpom that can arise from this symptom being untreated, if it exists.
                    else if (symptoms[i].Contains("<CollapseSymptomRef>"))
                    {
                        int start = symptoms[i].IndexOf("<CollapseSymptomRef>") + "<CollapseSymptomRef>".Length;
                        int end = symptoms[i].IndexOf("</CollapseSymptomRef>");

                        string collapseID = symptoms[i].Substring(start, end - start);
                        string collapse = (string)symptomTable[collapseID];

                        symptom.Collapse = collapse;
                    }
                    // Adds the the starting hours for the risk of a collapse.
                    else if (symptoms[i].Contains("<RiskOfCollapseStartHours>"))
                    {
                        int start = symptoms[i].IndexOf("<RiskOfCollapseStartHours>") + "<RiskOfCollapseStartHours>".Length;
                        int end = symptoms[i].IndexOf("</RiskOfCollapseStartHours>");
                        collapseStart = symptoms[i].Substring(start, end - start);

                        foundCollapseStart = true;
                    }
                    // Adds the ending hours for the risk of a collapse.
                    else if (symptoms[i].Contains("<RiskOfCollapseEndHours>"))
                    {
                        int start = symptoms[i].IndexOf("<RiskOfCollapseEndHours>") + "<RiskOfCollapseEndHours>".Length;
                        int end = symptoms[i].IndexOf("</RiskOfCollapseEndHours>");
                        collapseEnd = symptoms[i].Substring(start, end - start);

                        foundCollapseEnd = true;
                    }
                    // Adds the starting hours for the risk of death.
                    else if (symptoms[i].Contains("<RiskOfDeathStartHours>"))
                    {
                        int start = symptoms[i].IndexOf("<RiskOfDeathStartHours>") + "<RiskOfDeathStartHours>".Length;
                        int end = symptoms[i].IndexOf("</RiskOfDeathStartHours>");
                        deathStart = symptoms[i].Substring(start, end - start);

                        foundDeathStart = true;
                    }
                    // Adds the ending hours for the risk of death.
                    else if (symptoms[i].Contains("<RiskOfDeathEndHours>"))
                    {
                        int start = symptoms[i].IndexOf("<RiskOfDeathEndHours>") + "<RiskOfDeathEndHours>".Length;
                        int end = symptoms[i].IndexOf("</RiskOfDeathEndHours>");
                        deathEnd = symptoms[i].Substring(start, end - start);

                        foundDeathEnd = true;
                    }

                    // Add the collapse time frame.
                    if (foundCollapseStart == true && foundCollapseEnd == true)
                    {
                        foundCollapseStart = false;
                        foundCollapseEnd = false;

                        string collapseHours = collapseStart + " - " + collapseEnd;

                        symptom.CollapseTime = collapseHours;
                    }

                    // Add the death time frame.
                    if (foundDeathStart == true && foundDeathEnd == true)
                    {
                        foundDeathStart = false;
                        foundDeathEnd = false;

                        string deathHours = deathStart + " - " + deathEnd;;
                        symptom.DeathTime = deathHours;
                    }

                    i++;
                    if (symptom.SymptomName.Equals("ACL injury"))
                    current += i.ToString() + " ";

                    if (i >= symptoms.Length)
                    {
                        break;
                    }
                }

                if (i >= symptoms.Length)
                {
                    break;
                }

                symptomList.Add(nameID, symptom);
            }
        }

        static void processDiagnoses(string[] diagnoses, string department, DeptData data)
        {
            data.TotalDiseases = 0;

            for (int i = 0; i < diagnoses.Length; i++)
            {
                Disease disease = new Disease();

                // Set the department of the current disease.
                switch (department)
                {
                    case "emergency":
                        disease.Department = "Emergency";
                        break;
                    case "general-surgery":
                        disease.Department = "General Surgery";
                        break;
                    case "internal-medicine":
                        disease.Department = "Internal Medicine";
                        break;
                    case "orthopedics":
                        disease.Department = "Orthopedics";
                        break;
                    case "cardiology":
                        disease.Department = "Cardiology";
                        break;
                    case "neurology":
                        disease.Department = "Neurology";
                        break;
                }

                while (!diagnoses[i].Contains("</GameDBMedicalCondition>"))
                {
                    // Find the disease's name ID, and use that to get the string we retrieved earlier.
                    if (diagnoses[i].Contains("<GameDBMedicalCondition ID="))
                    {
                        int start = diagnoses[i].IndexOf("ID=\"") + "ID=\"".Length;
                        int end = diagnoses[i].IndexOf("\">");

                        string nameID = diagnoses[i].Substring(start, end - start);
                        disease.DiseaseName = (string)diagnosesTable[nameID];
                    }
                    // Find the disease's description ID, and use that to get the string we retireved earlier.
                    else if (diagnoses[i].Contains("<AbbreviationLocID>"))
                    {
                        int start = diagnoses[i].IndexOf("<AbbreviationLocID>") + "<AbbreviationLocID>".Length;
                        int end = diagnoses[i].IndexOf("</AbbreviationLocID>");

                        string nameID = diagnoses[i].Substring(start, end - start);
                        disease.Description = (string)diagnosesTable[nameID];
                    }
                    // Find the occurrence of the disease.
                    else if (diagnoses[i].Contains("Occurrence"))
                    {
                        if (diagnoses[i].Contains("COMMON"))
                        {
                            disease.Occurrence = "Common";
                        }
                        else if (diagnoses[i].Contains("UNCOMMON"))
                        {
                            disease.Occurrence = "Uncommon";
                        }
                        else
                        {
                            disease.Occurrence = "Rare";
                        }
                    }
                    // Find the base insurance payout of the diagnosis.
                    else if (diagnoses[i].Contains("<InsurancePayment>"))
                    {
                        int start = diagnoses[i].IndexOf("<InsurancePayment>") + "<InsurancePayment>".Length;
                        int end = diagnoses[i].IndexOf("</InsurancePayment>");

                        disease.Payment = diagnoses[i].Substring(start, end - start);
                    }
                    // Get all the symptoms associated with the disease.
                    else if (diagnoses[i].Contains("<Symptoms>"))
                    {
                        List<Symptom> symptoms = new List<Symptom>();
                        string probability = "";

                        // Loop through the symptom entry to get the probability and symptom reference ID.
                        while(!diagnoses[i].Contains("</Symptoms>"))
                        {
                            // Add the probability a patient will appear with the symptom
                            if (diagnoses[i].Contains("<ProbabilityPercent>"))
                            {
                                int start = diagnoses[i].IndexOf("<ProbabilityPercent>") + "<ProbabilityPercent>".Length;
                                int end = diagnoses[i].IndexOf("</ProbabilityPercent>");

                                probability = diagnoses[i].Substring(start, end - start);
                            }
                            // Retrieve the symptom from the symptomList based on the symptomID.
                            else if (diagnoses[i].Contains("<GameDBSymptomRef>"))
                            {
                                int start = diagnoses[i].IndexOf("<GameDBSymptomRef>") + "<GameDBSymptomRef>".Length;
                                int end = diagnoses[i].IndexOf("</GameDBSymptomRef>");

                                string symptomID = diagnoses[i].Substring(start, end - start);

                                Symptom temp = (Symptom)symptomList[symptomID];
                                
                                symptoms.Add(temp.clone());
                                symptoms[symptoms.Count - 1].Probability = probability;

                                // Used for data collection.
                                if (!symptomsDataList.Contains(symptoms[symptoms.Count - 1].SymptomName))
                                {
                                    symptomsDataList.Add(symptoms[symptoms.Count - 1].SymptomName);
                                }
                                
                                for (int j = 0; j < symptoms[symptoms.Count - 1].Examinations.Count; j++)
                                {
                                    if (!examsDataList.Contains(symptoms[symptoms.Count - 1].Examinations[j]))
                                    {
                                        examsDataList.Add(symptoms[symptoms.Count - 1].Examinations[j]);
                                    }
                                }
                            }

                            i++;
                        }

                        disease.Symptoms = symptoms;
                    }

                    i++;

                    if (i >= diagnoses.Length)
                    {
                        break;
                    }
                }

                if (i >= diagnoses.Length)
                {
                    break;
                }

                diseaseList.Add(disease);
                data.TotalDiseases++;
            }
        }

        static void Main(string[] args)
        {
            scrape();

            return;
        }
    }
}

