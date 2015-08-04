﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DataScienceECom;
using DataScienceECom.Models;
using DataScienceECom.Phis;
using NDSB.FileUtils;
using NDSB.Models.SparseModels;
using NDSB.SparseMappings;
using NDSB.SparseMethods;

namespace NDSB
{
    public partial class MainScreen : Form
    {
        public MainScreen()
        {
            InitializeComponent();
        }

        private void processBtn_Click(object sender, EventArgs e)
        {
            MessageBox.Show(IntPtr.Size.ToString());
        }


        private void button6_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Multiselect = true;
            fdlg.Title = "Files to merge";
            string[] filePaths = new string[1];
            if (fdlg.ShowDialog() == DialogResult.OK)
                filePaths = fdlg.FileNames;

            CSVHelper.ColumnBind(filePaths);
        }

        private void shuffleBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            int[] seeds = shuffleSeedTbx.Text.Split(';').Select(c => Convert.ToInt32(c)).ToArray();

            fdlg.Multiselect = true;
            fdlg.Title = "Files to shuffle";
            string[] filePaths = new string[1];
            if (fdlg.ShowDialog() == DialogResult.OK)
                filePaths = fdlg.FileNames;

            foreach (int seed in seeds)
                for (int i = 0; i < filePaths.Length; i++)
                    FShuffler.Shuffle(filePaths[i], seed);
        }

        private void splitBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            double part = Convert.ToDouble(splitTbx.Text);

            fdlg.Multiselect = true;
            fdlg.Title = "Files to split";
            string[] filePaths = new string[1];
            if (fdlg.ShowDialog() == DialogResult.OK)
                filePaths = fdlg.FileNames;

            for (int i = 0; i < filePaths.Length; i++)
                FSplitter.SplitRelative(filePaths[i], part);
        }

        private void downSampleBtn_Click_1(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Multiselect = true;
            fdlg.Title = "Files to down sample";
            string[] filePaths = new string[1];
            if (fdlg.ShowDialog() == DialogResult.OK)
                filePaths = fdlg.FileNames;

            int[] eltsPerClass = downSampleTbx.Text.Split(';').Select(c => Convert.ToInt32(c)).ToArray();
            for (int i = 0; i < eltsPerClass.Length; i++)
                for (int j = 0; j < filePaths.Length; j++)
                    DownSample.Run(filePaths[j], eltsPerClass[i], DSCdiscountUtils.GetLabelCDiscountDB);
        }

        private void nearestCentroidPredictBtn_Click(object sender, EventArgs e)
        {
            string[] trainFilePaths = new string[1];
            string testFilePath = "";

            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Title = "Test file path";
            if (fdlg.ShowDialog() == DialogResult.OK)
                testFilePath = fdlg.FileName;

            fdlg = new OpenFileDialog();
            fdlg.Multiselect = true;
            fdlg.Title = "Train file(s) path";
            if (fdlg.ShowDialog() == DialogResult.OK)
                trainFilePaths = fdlg.FileNames;

            for (int i = 0; i < trainFilePaths.Length; i++)
            {
                List<NearestCentroid> models = new List<NearestCentroid>();
                models.Add(new NearestCentroid(new PureInteractions(1, 20)));
                GenericMLHelper.TrainPredictAndWrite(models.ToArray(), trainFilePaths[i], testFilePath);
                models.Clear();
            }
        }

        private void getHistogramBtn_Click(object sender, EventArgs e)
        {
            string trainFilePath = "";
            OpenFileDialog fdlg = new OpenFileDialog();

            if (fdlg.ShowDialog() == DialogResult.OK)
                trainFilePath = fdlg.FileName;

            int[] labels = DSCdiscountUtils.ReadLabelsFromTraining(trainFilePath);

            EmpiricScore<int> es = new EmpiricScore<int>();
            for (int i = 0; i < labels.Length; i++)
                es.UpdateKey(labels[i], 1);

            //es = es.Normalize();

            string[] text = es.Scores.OrderBy(c => c.Value).Select(c => c.Key + " " + c.Value).ToArray();
            File.WriteAllLines(Path.GetDirectoryName(trainFilePath) + "\\" + Path.GetFileNameWithoutExtension(trainFilePath) + "_hist.txt", text);

            MessageBox.Show("h");
        }

        private void predictSGDBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog trainingFilesOFD = new OpenFileDialog();
            trainingFilesOFD.Multiselect = true;
            trainingFilesOFD.Title = "Train files path";
            trainingFilesOFD.ShowDialog();
            if (!trainingFilesOFD.CheckFileExists) return;

            OpenFileDialog validationFilesOFD = new OpenFileDialog();
            validationFilesOFD.Multiselect = true;
            validationFilesOFD.Title = "Validation files path";
            validationFilesOFD.ShowDialog();

            if (!validationFilesOFD.CheckFileExists) return;

            OpenFileDialog testFileOFD = new OpenFileDialog();
            testFileOFD.Title = "Test file path";
            testFileOFD.ShowDialog();

            string currentDirectory = Path.GetDirectoryName(trainingFilesOFD.FileNames[0]),
                testFilePath = testFileOFD.FileName,
                cvFilePath = currentDirectory + "\\CrossValidation.csv";

            List<Phi> phis = new List<Phi> { Phis.phi16, Phis.phi13 };

            string[] learningFiles = trainingFilesOFD.FileNames;
            Array.Sort(learningFiles);
            string[] validationFiles = validationFilesOFD.FileNames;
            Array.Sort(validationFiles);

            foreach (IStreamingModel model in ModelGenerators.Entropia4())
                for (int i = 0; i < learningFiles.Length; i++)
                    foreach (Phi phi in phis)
                    {
                        string file = learningFiles[i];
                        model.ClearModel();
                        TrainModels.TrainStreamingModel(model, phi, file);
                        string modelString = Path.GetFileNameWithoutExtension(file) + ";" + Path.GetFileNameWithoutExtension(validationFiles[i]) + ";" +
                            phi.Method.Name + ";" + model.ToString();

                        List<int> testModelPredictions = TrainModels.Predict(model, phi, testFilePath);

                        File.WriteAllText(Path.GetDirectoryName(file) + "\\submissions\\" +
                            modelString + "_pred.csv", modelString + Environment.NewLine);

                        File.AppendAllLines(Path.GetDirectoryName(file) + "\\submissions\\" +
                            modelString + "_pred.csv",
                            testModelPredictions.Select(t => t.ToString()));

                        string validationFileName = validationFiles[i];

                        var validationModelPredictions = TrainModels.Validate(model, phi, validationFileName);

                        File.WriteAllText(Path.GetDirectoryName(file) + "\\validations\\" +
                            modelString + "_val.csv",
                            modelString + Environment.NewLine);

                        File.AppendAllLines(Path.GetDirectoryName(file) + "\\validations\\" +
                            modelString + "_val.csv",
                            validationModelPredictions.Item1.Select(t => t.ToString()));

                        File.AppendAllText(cvFilePath, modelString + ";" + (validationModelPredictions.Item2 * 100f).ToString() + Environment.NewLine);
                    }
        }



        private void splitTbx_TextChanged(object sender, EventArgs e)
        {

        }

        private static int[] ToIntArray(TextBox tbx)
        {
            return tbx.Text.Split(';').Select(c => Convert.ToInt32(c)).ToArray();
        }


        private void NCTrainValidatePredictBtn_Click(object sender, EventArgs e)
        {
            string testFilePath = "",
                validationFilePath = "";
            string[] trainFilePath = new string[0];

            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Title = "Test file path";
            if (fdlg.ShowDialog() == DialogResult.OK)
                testFilePath = fdlg.FileName;

            fdlg = new OpenFileDialog();
            fdlg.Title = "Train file(s) path";
            fdlg.Multiselect = true;
            if (fdlg.ShowDialog() == DialogResult.OK)
                trainFilePath = fdlg.FileNames;

            fdlg = new OpenFileDialog();
            fdlg.Title = "Validation file path";
            if (fdlg.ShowDialog() == DialogResult.OK)
                validationFilePath = fdlg.FileName;

            for (int i = 0; i < trainFilePath.Length; i++)
            {
                List<IModelClassification<Dictionary<string, double>>> models = new List<IModelClassification<Dictionary<string, double>>>();

                models.Add(new DecisionTree(4500, 10));

                models.Add(new NearestCentroid(new PureInteractions(1, 20)));

                models.Add(new KNN(Distances.Euclide, 1, 0.2, new ToSphere()));

                models.Add(new KNN(Distances.Euclide, 4, 0.2, new ToSphere()));

                GenericMLHelper.TrainPredictAndValidate(models.ToArray(), testFilePath, trainFilePath[i], validationFilePath);
            }

        }

        private void extractColumnBtn_Click(object sender, EventArgs e)
        {
            string filePath = "";
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Title = "File path";
            if (fdlg.ShowDialog() == DialogResult.OK)
                filePath = fdlg.FileName;

            int columnIndex = Convert.ToInt32(extractColumnTbx.Text);

            CSVHelper.ExtractColumn(filePath, columnIndex);

        }
    }
}
