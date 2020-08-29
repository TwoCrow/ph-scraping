// Created by Patrick Sherbondy

// 8/21/2020: Added new information to diagnoses. Added new output formatting option for Google Drive.
// 8/18/2020: Added new symptoms, treatments, and diagnoses from the Infectious Disease DLC.
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
        public int Probability { get; set; }
        public List<string> Examinations { get; set; }
        public string Treatment { get; set; }
        public string Collapse { get; set; }
        public string CollapseTime { get; set; }
        public string DeathTime { get; set; }
        public string Hazard { get ; set; }

        public Symptom()
        {
            SymptomName = " ";
            Probability = 0;
            Examinations = new List<string>();
            Treatment = "None";
            Collapse = "N/A";
            CollapseTime = "N/A";
            DeathTime = "N/A";
            Hazard = "None";
        }

        public Symptom(string symptom, int probability, List<string> examinations, string treatment, string collapse, string collapseTime, string deathTime, string hazard)
        {
            SymptomName = symptom;
            Probability = probability;
            Examinations = examinations;
            Treatment = treatment;
            Collapse = collapse;
            CollapseTime = collapseTime;
            DeathTime = deathTime;
            Hazard = Hazard;
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
        public int TotalFatalDiseases { get; set; }
        public Dictionary<string, int> DecidingExams { get; set; }
        // Exams that uncover a symptom capable of causing a collapse.
        public Dictionary<string, int> UrgentExams { get; set; }

        public DeptData(string name)
        {
            DeptName = name;
            TotalDiseases = 0;
            TotalFatalDiseases = 0;
            DecidingExams = new Dictionary<string, int>();
            UrgentExams = new Dictionary<string, int>();
        }
    }

    class Scrape
    {
        // Keeps a running list (yes I know it's a hashtable) of all possible symptoms in the game and their various stats.
        public static Hashtable symptomList = new Hashtable();
        // Keeps a list of symptoms that can cause death.
        public static HashSet<string> fatalSymptoms = new HashSet<string>();
        // Keeps a list of all examinations.
        public static List<string> examsList = new List<string>();
        // Keeps a list of all treatments.
        public static List<string> treatmentList = new List<string>();
        // Creates a dictionary that pairs the in-game exam names with grammatically correct exam names.
        public static Dictionary<string, string> examPairs = new Dictionary<string, string>();
        // This dictionary is similar to the previous one, though is used for treatments.
        public static Dictionary<string, string> treatmentPairs = new Dictionary<string, string>();
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
        public static int totalDiagnoses = 0;
        
        static void scrape()
        {
            string[] er = System.IO.File.ReadAllLines(@".\Diagnoses\DiagnosesER.txt", Encoding.UTF8);
            string[] surg = System.IO.File.ReadAllLines(@".\Diagnoses\DiagnosesSURG.txt", Encoding.UTF8);
            string[] intern = System.IO.File.ReadAllLines(@".\Diagnoses\DiagnosesINTERN.txt", Encoding.UTF8);
            string[] ortho = System.IO.File.ReadAllLines(@".\Diagnoses\DiagnosesORTHO.txt", Encoding.UTF8);
            string[] cardio = System.IO.File.ReadAllLines(@".\Diagnoses\DiagnosesCARDIO.txt", Encoding.UTF8);
            string[] neuro = System.IO.File.ReadAllLines(@".\Diagnoses\DiagnosesNEURO.txt", Encoding.UTF8);
            string[] infectious = System.IO.File.ReadAllLines(@".\Diagnoses\DiagnosesINFECT.txt", Encoding.UTF8);

            string[] symptoms1 = System.IO.File.ReadAllLines(@".\Symptoms\Symptoms_main.txt", Encoding.UTF8);
            string[] symptoms2 = System.IO.File.ReadAllLines(@".\Symptoms\Symptoms.txt", Encoding.UTF8);

            // Used to gather data on the various departments.
            DeptData erData = new DeptData("emergency");
            DeptData surgData = new DeptData("general-surgery");
            DeptData internData = new DeptData("internal-medicine");
            DeptData orthoData = new DeptData("orthopedics");
            DeptData cardioData = new DeptData("cardiology");
            DeptData neuroData = new DeptData("neurology");
            DeptData infectiousData = new DeptData("infectious-diseases");
            
            // Processes the four files in the Localization folder, allowing for the proper in-game names and descriptions
            // to appear in the final output.
            processTable("DiagnosesTable.txt", diagnosesTable, true);
            processTable("ExamTable.txt", examTable, false);
            processTable("SymptomTable.txt", symptomTable, false);
            processTable("TreatmentTable.txt", treatmentTable, false);

            // Processes all possible symptoms in the game so that they can be paired with various diagnoses.
            // There are two symptoms variables because the game decided to break up its symptoms into two files, so this makes life easier.
            // Except it doesn't, because subsequent DLC files have symptoms, diagnoses, and treatments just scattered all about the place.
            processSymptoms(symptoms1);
            processSymptoms(symptoms2);

            // Used to create pleasing grammatically-correct sentences in the final output files.
            generateExamsAndTreatments();
            createGrammaticalPairs();

            // Backs up the previous output files. This is used for comparative purposes after the program runs.
            backupPreviousOutputFiles("emergency");
            backupPreviousOutputFiles("general-surgery");
            backupPreviousOutputFiles("internal-medicine");
            backupPreviousOutputFiles("orthopedics");
            backupPreviousOutputFiles("cardiology");
            backupPreviousOutputFiles("neurology");
            backupPreviousOutputFiles("infectious-diseases");

            // Processes the individual diseases for each department, bringing all the hard work from the previous
            // methods together.
            processDiagnoses(er, "emergency", erData);
            generateDataFile(erData);
            generateOutputFile("emergency");

            processDiagnoses(surg, "general-surgery", surgData);
            generateDataFile(surgData);
            generateOutputFile("general-surgery");

            processDiagnoses(intern, "internal-medicine", internData);
            generateDataFile(internData);
            generateOutputFile("internal-medicine");

            processDiagnoses(ortho, "orthopedics", orthoData);
            generateDataFile(orthoData);
            generateOutputFile("orthopedics");

            processDiagnoses(cardio, "cardiology", cardioData);
            generateDataFile(cardioData);
            generateOutputFile("cardiology");

            processDiagnoses(neuro, "neurology", neuroData);
            generateDataFile(neuroData);
            generateOutputFile("neurology");

            processDiagnoses(infectious, "infectious-diseases", infectiousData);
            generateDataFile(infectiousData);
            generateOutputFile("infectious-diseases");

            // Merges the output files for easy copy / pasting.
            //mergeOutputFiles();
            // Compares the old and new files and creates a separate file noting any changes.
            compareFiles("emergency");
            compareFiles("general-surgery");
            compareFiles("internal-medicine");
            compareFiles("orthopedics");
            compareFiles("cardiology");
            compareFiles("neurology");
            compareFiles("infectious-diseases");

            Console.WriteLine("Total diagnoses: {0}", totalDiagnoses);
        }

        static void compareFiles(string dept)
        {
            string oldPath = @".\Output\Previous\previous-" + dept + "-diagnoses.txt";
            string newPath = @".\Output\Formatted-Diagnoses\" + dept + "-diagnoses.txt";

            string count1 = File.ReadAllText(oldPath);
            string count2 = File.ReadAllText(newPath);

            string[] oldFile = File.ReadAllLines(oldPath);
            string[] newFile = File.ReadAllLines(newPath);

            // A very simple, homebrew way of notifying me of any changes between files.
            // It's very unlikely that any significant changes to the content of a file would make it the same
            // length as the old one.
            if (count1.Length != count2.Length)
            {
                Console.WriteLine("! Difference noticed in {0}.txt !", dept);
            }
        }

        static void mergeOutputFiles()
        {
            string file = @".\Output\Merged\comprehensive-diagnoses-list.txt";

            if (File.Exists(file))
            {
                File.Delete(file);
            }

            string[] er = System.IO.File.ReadAllLines(@".\Output\Formatted-Diagnoses\emergency-diagnoses.txt");
            string[] surg = System.IO.File.ReadAllLines(@".\Output\Formatted-Diagnoses\general-surgery-diagnoses.txt");
            string[] intern = System.IO.File.ReadAllLines(@".\Output\Formatted-Diagnoses\internal-medicine-diagnoses.txt");
            string[] ortho = System.IO.File.ReadAllLines(@".\Output\Formatted-Diagnoses\orthopedics-diagnoses.txt");
            string[] cardio = System.IO.File.ReadAllLines(@".\Output\Formatted-Diagnoses\cardiology-diagnoses.txt");
            string[] neuro = System.IO.File.ReadAllLines(@".\Output\Formatted-Diagnoses\neurology-diagnoses.txt");
            string[] infectious = System.IO.File.ReadAllLines(@".\Output\Formatted-Diagnoses\infectious-diseases-diagnoses.txt");

            File.AppendAllLines(file, er);
            File.AppendAllLines(file, surg);
            File.AppendAllLines(file, intern);
            File.AppendAllLines(file, ortho);
            File.AppendAllLines(file, cardio);
            File.AppendAllLines(file, neuro);
            File.AppendAllLines(file, infectious);
        }

        // Backs up the 
        static void backupPreviousOutputFiles(string dept)
        {
            string destination = @".\Output\Previous\previous-" + dept + "-diagnoses.txt";
            string source = @".\Output\Formatted-Diagnoses\" + dept +"-diagnoses.txt";

            if (File.Exists(destination))
            {
                File.Delete(destination);
            }

            File.Copy(source, destination);
        }

        static void createGrammaticalPairs()
        {
            string customFile = @".\Output\exams-and-treatments.txt";
            string gameFile = @".\Output\exams-and-treatments2.txt";

            string[] customText = File.ReadAllLines(customFile);
            string[] gameText = File.ReadAllLines(gameFile);

            // This starts at 2 to bypass two unnecessary lines.
            // Pairs up game and custom exam names.
            for (int i = 2; i < 65; i++)
            {
                examPairs[gameText[i]] = customText[i];
            }

            for (int i = 68; i < gameText.Length; i++)
            {
                treatmentPairs[gameText[i]] = customText[i];
            }
        }

        static void generateExamsAndTreatments()
        {
            string file = @".\Output\exams-and-treatments2.txt";

            if (File.Exists(file))
            {
                File.Delete(file);
            }

            FileStream ofs = File.Create(file);
            StreamWriter ofp = new StreamWriter(ofs);

            ofp.WriteLine("Exams\n");

            HashSet<string> examSet = new HashSet<string>();
            HashSet<string> treatmentSet = new HashSet<string>();

            foreach (string exam in examsList)
            {
                if (!examSet.Contains(exam))
                {
                    ofp.WriteLine(exam);
                }
                    
                examSet.Add(exam);
            }

            ofp.WriteLine("\nTreatments\n");

            foreach (string treatment in treatmentList)
            {
                if (!treatmentSet.Contains(treatment))
                {
                    ofp.WriteLine(treatment);
                }
                
                treatmentSet.Add(treatment);
            }
            
            ofp.Flush();
            ofp.Close();
        }

        static void generateDataFile(DeptData data)
        {
            if (!getDeptData)
            {
                return;
            }

            string file = @".\Output\Department-Data\" + data.DeptName + "-department-data.txt";

            if (File.Exists(file))
            {
                File.Delete(file);
            }

            FileStream ofs = File.Create(file);
            StreamWriter ofp = new StreamWriter(ofs);

            // We only want to count exams once per diagnosis, since an exam run on a patient will uncover all possible symptoms. If we increment for each symptom
            // an exam can uncover, we're effectively counting it multiple times.
            HashSet<String> seenDecidingExams = new HashSet<string>();
            HashSet<String> seenUrgentExams = new HashSet<string>();

            bool visitedDiagnosis = false;

            // Iterate through the diagnoses that have been processed for the current department, finding the ones that are guaranteed to appear and can cause
            // collapse symptoms.
            for (int i = 0; i < diseaseList.Count; i++)
            {
                for (int j = 0; j < diseaseList[i].Symptoms.Count; j++)
                {
                    for (int k = 0; k < diseaseList[i].Symptoms[j].Examinations.Count; k++)
                    {   
                        string exam = diseaseList[i].Symptoms[j].Examinations[k];
                        // If the probability equals 100, add it to the DecidingExams dictionary.
                        if (diseaseList[i].Symptoms[j].Probability == 100 && !seenDecidingExams.Contains(exam))
                        {
                            if (data.DecidingExams.ContainsKey(exam))
                            {
                                data.DecidingExams[exam] = data.DecidingExams[exam] + 1;
                            }
                            else
                            {
                                data.DecidingExams[exam] = 1;
                            }

                            seenDecidingExams.Add(exam);
                        }

                        // If the symptom can cause death or lead to a collapse, add the exam to the UrgentExams dictionary.
                        if ((!diseaseList[i].Symptoms[j].DeathTime.Equals("N/A") || !diseaseList[i].Symptoms[j].Collapse.Equals("N/A")) && !seenUrgentExams.Contains(exam))
                        {
                            // Add exam to UrgentExams, since it will uncover a symptom that can cause a collapse or death.
                            if (data.UrgentExams.ContainsKey(exam))
                            {
                                data.UrgentExams[exam] = data.UrgentExams[exam] + 1;
                            }
                            else
                            {
                                data.UrgentExams[exam] = 1;
                            }

                            seenUrgentExams.Add(exam);
                        }

                        if ((!diseaseList[i].Symptoms[j].DeathTime.Equals("N/A") || fatalSymptoms.Contains(diseaseList[i].Symptoms[j].Collapse)) && !visitedDiagnosis)
                        {
                            data.TotalFatalDiseases++;
                            visitedDiagnosis = true;
                        }
                    }
                }

                // Reset the hash sets.
                seenDecidingExams.Clear();
                seenUrgentExams.Clear();

                visitedDiagnosis = false;
            }

            ofp.WriteLine("Total Diagnoses: {0}\nPotentially-Fatal Diagnoses: {1}\n", data.TotalDiseases, data.TotalFatalDiseases);

            ofp.WriteLine("Deciding Exam Frequency:");

            var list1 = from pair in data.DecidingExams orderby pair.Value descending select pair;

            var list2 = from pair in data.UrgentExams orderby pair.Value descending select pair;

            foreach(KeyValuePair<string, int> pair in list1)
            {
                ofp.WriteLine("{0}: {1}", pair.Key, pair.Value);
            }

            ofp.WriteLine("\nUrgent Exam Frequency:");

            foreach(KeyValuePair<string, int> pair in list2)
            {
                ofp.WriteLine("{0}: {1}", pair.Key, pair.Value);
            }

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

        static string normalize(string temp)
        {
            TextInfo info = new CultureInfo("en-US", false).TextInfo;

            temp = info.ToTitleCase(temp);

            if (temp.Contains("And"))
            {
                temp = temp.Replace("And", "and");
            }

            if (temp.Contains("Of"))
            {
               temp = temp.Replace("Of", "of");
            }

            if (temp.Contains("The"))
            {
                temp = temp.Replace("The", "the");
            }

            if (temp.Contains("’S"))
            {
                temp = temp.Replace("’S", "’s");
            }

            if (temp.Contains("hearbeat"))
            {
                temp = temp.Replace("hearbeat", "Heartbeat");
            }

            return temp;
        }

        static void generateOutputFile(String deptName)
        {
            string file = @".\Output\Formatted-Diagnoses\" + deptName +"-diagnoses.txt";
            // Used to select which format is desired.
            bool useSteamFormat = false, usePlainFormat = false, useDriveFormat = true;

            if (File.Exists(file))
            {
                File.Delete(file);
            }

            FileStream ofs = File.Create(file);
            StreamWriter ofp = new StreamWriter(ofs);

            // Sort the list alphabetically.
            diseaseList.Sort(delegate(Disease x, Disease y)
            {
                if (x.DiseaseName == null && y.DiseaseName == null) 
                    return 0;
                else if (x.DiseaseName == null) 
                    return -1;
                else if (y.DiseaseName == null) 
                    return -1;
                else 
                    return x.DiseaseName.CompareTo(y.DiseaseName);
            });

            // Generates a text file with steam-friendly formatting.
            if (useSteamFormat)
            {
                for (int i = 0; i < diseaseList.Count; i++)
                {
                    string temp = diseaseList[i].DiseaseName;
                    TextInfo info = new CultureInfo("en-US", false).TextInfo;

                    ofp.WriteLine("[u][b][h1]{0}[/h1][/b][/u]", info.ToTitleCase(temp));
                    ofp.WriteLine("[quote]{0}[/quote]", diseaseList[i].Description);
                    
                    ofp.WriteLine("");

                    //ofp.WriteLine("Department: {0}", diseaseList[i].Department);
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
            // Generates a plaintext file without any special formatting.
            else if (usePlainFormat)
            {
                for (int i = 0; i < diseaseList.Count; i++)
                {
                    string temp = diseaseList[i].DiseaseName;
                    TextInfo info = new CultureInfo("en-US", false).TextInfo;

                    ofp.WriteLine("{0}", info.ToTitleCase(temp));
                    ofp.WriteLine("{0}", diseaseList[i].Description);
                    
                    ofp.WriteLine("");

                    //ofp.WriteLine("Department: {0}", diseaseList[i].Department);
                    ofp.WriteLine("Occurrence: {0}", diseaseList[i].Occurrence);
                    ofp.WriteLine("Base Payment: {0}", diseaseList[i].Payment);

                    ofp.WriteLine("");

                    // Sort the symptoms list by probability, with the symptoms most likely to appear being at the top.
                    diseaseList[i].Symptoms.Sort(delegate(Symptom x, Symptom y)
                    {
                        return y.Probability.CompareTo(x.Probability);
                    });

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
            else if (useDriveFormat)
            {
                ofp.WriteLine("# {0}\n", diseaseList[0].Department);
                for (int i = 0; i < diseaseList.Count; i++)
                {
                    string temp = diseaseList[i].DiseaseName;
                    
                    temp = normalize(temp);

                    ofp.WriteLine("## {0} ({1} | ${2})", temp, diseaseList[i].Occurrence, diseaseList[i].Payment);
                    ofp.WriteLine("");
                    ofp.WriteLine("{0}", diseaseList[i].Description);
                    ofp.WriteLine("");

                    // Sort the symptoms list by probability, with the symptoms most likely to appear being at the top.
                    diseaseList[i].Symptoms.Sort(delegate(Symptom x, Symptom y)
                    {
                        return y.Probability.CompareTo(x.Probability);
                    });

                    ofp.WriteLine("### Symptoms");
                    ofp.WriteLine("");

                    bool isSerious = false;
                    bool isFatal = false;

                    // Loop through each symptom for the current disease.
                    for (int j = 0; j < diseaseList[i].Symptoms.Count; j++)
                    {
                        Symptom symptom = diseaseList[i].Symptoms[j];

                        //Console.WriteLine("!" + symptom.DeathTime + "!");

                        // If the symptom cannot cause a collapse, use this format.
                        if (symptom.Collapse.Equals("N/A") && symptom.DeathTime.Equals("N/A"))
                        {
                            ofp.WriteLine("+ {0} ({1}% of cases | {2} Hazard)", normalize(symptom.SymptomName), symptom.Probability, symptom.Hazard);
                        }
                        // If the symptom can cause a collapse, then use this format.
                        else if (!symptom.Collapse.Equals("N/A"))
                        {
                            ofp.WriteLine("+ {0} ({1}% of cases | {2} Hazard | __Serious__)", normalize(symptom.SymptomName), symptom.Probability, symptom.Hazard);

                            isSerious = true;
                        }
                        else if (!symptom.DeathTime.Equals("N/A"))
                        {
                            ofp.WriteLine("+ {0} ({1}% of cases | {2} Hazard | __Fatal__)", normalize(symptom.SymptomName), symptom.Probability, symptom.Hazard);

                            isFatal = true;
                        }

                        // Prints out the possible exams that will reveal the symptom.
                        if (symptom.Examinations.Count == 1)
                        {
                            ofp.WriteLine("    + Revealed with __{0}__.", examPairs[symptom.Examinations[0]]);
                        }
                        else if (symptom.Examinations.Count == 2)
                        {
                            ofp.WriteLine("    + Revealed with __{0}__ or __{1}__.", examPairs[symptom.Examinations[0]], examPairs[symptom.Examinations[1]]);
                        }
                        else
                        {
                            ofp.WriteLine("    + Revealed with __{0}__, __{1}__, or __{2}__.", examPairs[symptom.Examinations[0]], examPairs[symptom.Examinations[1]], examPairs[symptom.Examinations[2]]);
                        }

                        // Prints out the treatment used to suppress the symptom.
                        if (!symptom.Treatment.Equals("None"))
                        {
                            ofp.WriteLine("    + Treated with __{0}__.", treatmentPairs[symptom.Treatment]);
                        }
                        else
                        {
                            ofp.WriteLine("    + Cannot be treated.");
                        }

                        // If the symptom is serious, denote the collapse symptom and its hours.
                        if (isSerious)
                        {
                            ofp.WriteLine("    + Can lead to __{0}__ if left untreated for __{1}__ hours.", symptom.Collapse, symptom.CollapseTime);
                        }

                        if (isFatal)
                        {
                            ofp.WriteLine("    + Can lead to __death__ if left untreated for __{0}__ hours.", symptom.DeathTime);
                        }

                        isSerious = false;
                        isFatal = false;
                    }

                    ofp.WriteLine("");
                }
            }
            
            // Clear the disease list for the next input file.
            diseaseList.Clear();
            
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
                                examsList.Add(exam);
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
                        treatmentList.Add(treatment);
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
                    // Adds the hazard level for the symptom.
                    else if (symptoms[i].Contains("<Hazard>"))
                    {
                        int start = symptoms[i].IndexOf("<Hazard>") + "<Hazard>".Length;
                        int end = symptoms[i].IndexOf("</Hazard>");
                        string hazard = symptoms[i].Substring(start, end - start);

                        symptom.Hazard = hazard;
                    }

                    // Add the collapse time frame.
                    if (foundCollapseStart == true && foundCollapseEnd == true)
                    {
                        foundCollapseStart = false;
                        foundCollapseEnd = false;

                        string collapseHours = collapseStart + " to " + collapseEnd;

                        symptom.CollapseTime = collapseHours;
                    }

                    // Add the death time frame.
                    if (foundDeathStart == true && foundDeathEnd == true)
                    {
                        foundDeathStart = false;
                        foundDeathEnd = false;

                        string deathHours = deathStart + " to " + deathEnd;;
                        symptom.DeathTime = deathHours;

                        fatalSymptoms.Add(symptom.SymptomName);
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
                    case "infectious-diseases":
                        disease.Department = "Infectious Diseases";
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
                                symptoms[symptoms.Count - 1].Probability = Int32.Parse(probability);

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
                totalDiagnoses++;
            }
        }

        static void Main(string[] args)
        {
            scrape();

            return;
        }
    }
}

