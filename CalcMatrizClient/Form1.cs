using CalcMatrizClient.Models;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Text.Json;
using System.Text;
using System;

namespace CalcMatrizClient
{
    public partial class Form1 : Form
    {
        private double[,] matrixResult;
        private double[,] matrixResult2;
        private object matrixLock = new object();
        private Stopwatch watch = new Stopwatch();
        private static List<string> matrixes = new List<string>();
        private static readonly HttpClient client = new HttpClient();


        public Form1()
        {
            InitializeComponent();
        }

        private void btCarregaMatriz_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            DialogResult result = openFileDialog1.ShowDialog();
            // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                string file = openFileDialog1.FileName;

                try
                {
                    if (matrixes.Count < 2)
                        matrixes.Add(file);

                    if (matrixes.Count == 2)
                    {
                        btCalculaMatriz.Enabled = true;
                    }

                }
                catch (IOException)
                {
                }
            }
        }

        private async void btCalculaMatriz_Click(object sender, EventArgs e)
        {
            watch.Start();

            var fileMatrix1 = matrixes[0];

            var fileMatrix2 = matrixes[1];


            var fileRead1 = ReadFile(fileMatrix1);

            var fileRead2 = ReadFile(fileMatrix2);

            var matrixA = ConvertToDouble(fileRead1);

            var matrixB = ConvertToDouble(fileRead2);

            int rows = matrixA.GetLength(0);
            int cols = matrixA.GetLength(1);

            matrixResult = new double[rows, cols];

            //call grpc to multiply matrix
            //MultiplyMatrixes(matrixA, matrixB, 0, matrixA.Length);

            matrixResult = await GetDataFromApi(matrixA, matrixB, 0, matrixA.Length);

            matrixes.Clear();

            btCalculaMatriz.Enabled = false;

            GenerateFile(matrixResult);

            watch.Stop();

            MessageBox.Show($"Tempo para realizar a execução em minutos: {watch.Elapsed.TotalMinutes.ToString()}");
            MessageBox.Show($"Tempo para realizar a execução em segundos: {watch.Elapsed.TotalSeconds.ToString()}");
            MessageBox.Show($"Tempo para realizar a execução em milisegundos: {watch.Elapsed.TotalMilliseconds.ToString()}");
        }

        private async Task<double[,]> GetDataFromApi(double[,] matrixA, double[,] matrixB, int index, int len)
        {
            string apiUrl = "http://localhost:5244/Matriz";

            var matrixModel = new MatrixModel
            {
                matrixA = ConvertToJaggedArray(matrixA),
                matrixB = ConvertToJaggedArray(matrixB),
                index = index,
                length = len
            };

            try
            {

                client.Timeout = TimeSpan.FromMinutes(120);

                string jsonContent = JsonSerializer.Serialize<MatrixModel>(matrixModel);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Send GET request to API
                HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                response.EnsureSuccessStatusCode();

                // Read response as a string
                string responseBody = await response.Content.ReadAsStringAsync();

                // Deserialize JSON response to MatrixReturn object
                var matrixResult = JsonSerializer.Deserialize<MatrixReturn>(responseBody);

                return ConvertTo2DArray(matrixResult.matrixResult);

            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Request error: {ex.Message}");
                throw ex;
            }
        }

        private void GenerateFile(double[,] result)
        {
            var rows = result.GetLength(0);
            var cols = result.GetLength(1);

            var path = $"C:/Users/lucas/Downloads/results/matResult-{DateTime.Now.ToString("HH-mm-ss")}.txt";
            using (var sw = new System.IO.StreamWriter(path))
            {

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        sw.Write(result[i, j].ToString() + " ");
                    }
                    sw.WriteLine();
                }
            }

            Array.Clear(matrixResult);
            MessageBox.Show($"Arquivo criado {path}");
        }

        private void MultiplyMatrixes(double[,] matrixA, double[,] matrixB, int index, int len)
        {
            int rows = matrixA.GetLength(0);
            int cols = matrixA.GetLength(1);

            // Create the result matrix with appropriate dimensions (rowsA x colsB)
            double[,] resultMatrix = new double[rows, cols];

            // Perform matrix multiplication
            for (int i = 0; i < rows; i++)      // Loop through rows of matrix A
            {
                for (int j = 0; j < cols; j++)   // Loop through columns of matrix B
                {
                    double result = 0;
                    for (int k = 0; k < cols; k++)   // Loop through columns of matrix A / rows of matrix B
                    {
                        result += matrixA[i, k] * matrixB[k, j];
                    }
                    resultMatrix[i, j] = Math.Round(result, 4);
                }
            }

            Array.Copy(resultMatrix, 0, matrixResult, index, len);
        }

        private double[,] ConvertToDouble(string[] fileRead)
        {
            int rows = fileRead.Length; ;
            int collumns = fileRead[0].Split(' ').Length;


            double[,] matrix = new double[rows, collumns];

            for (int i = 0; i < rows; i++)
            {

                var lineReplaced = fileRead[i].Replace('.', ',');
                var lineDouble = lineReplaced.Split(' ');

                // Convert each item in the line to a double and store it in the array
                for (int j = 0; j < collumns; j++)
                {
                    matrix[i, j] = Convert.ToDouble(lineDouble[j]);
                }

            }

            return matrix;
        }

        private string[] ReadFile(string fileMatrix)
        {
            return File.ReadAllLines(fileMatrix);
        }

        public static double[][] ConvertToJaggedArray(double[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            double[][] jaggedArray = new double[rows][];

            for (int i = 0; i < rows; i++)
            {
                jaggedArray[i] = new double[cols];
                for (int j = 0; j < cols; j++)
                {
                    jaggedArray[i][j] = matrix[i, j];
                }
            }

            return jaggedArray;
        }

        public static double[,] ConvertTo2DArray(double[][] jaggedArray)
        {
            int rows = jaggedArray.Length;
            int cols = jaggedArray[0].Length;
            double[,] matrix = new double[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    matrix[i, j] = jaggedArray[i][j];
                }
            }

            return matrix;
        }
    }
}
