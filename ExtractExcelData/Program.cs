﻿using ClosedXML.Excel;
using ExtractExcelData.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ExtractExcelData
{
    internal class Program
    {
        //static void Main(string[] args)
        //{
        //    Console.WriteLine("------------------- Start ----------------");

        //    // Read Excel Data
        //    Console.WriteLine("Reading records from excel started...");
        //    string filePath = @"D:\Temp\Akshay\ExtractDataFromExcel\ExtractExcelData\Excel\Input\ParcelData.xlsx";
        //    var parcelRecords = ReadParcelData(filePath);
        //    Console.WriteLine("Reading records from excel ends...");


        //    foreach (var parcelRecord in parcelRecords)
        //    {

        //        Console.WriteLine($"------------------- Scrapping start for parcel number - {parcelRecord.ParcelNumber}  ----------------");
        //        WebScrapper(parcelRecord);
        //        Console.WriteLine($"-------------------  Scrapping end for parcel number - {parcelRecord.ParcelNumber}  ----------------");

        //        Console.WriteLine("Save page as pdf started...");
        //        SavePageAsPdf(parcelRecord.ParcelNumber, parcelRecord.DocName);
        //        Console.WriteLine("Save page as pdf ends...");

        //    }




        //    // Save Output data to excel
        //    Console.WriteLine("Getting Payment Histories started...");
        //    var paymentHistories = GetAllPaymentHistories();
        //    Console.WriteLine("Getting Payment Histories ends...");


        //    Console.WriteLine("------------------- Saving Output data to excel stared ----------------");
        //    SaveToExcel(paymentHistories);
        //    Console.WriteLine("------------------- Saving Output data to excel ends ----------------");

        //    Console.WriteLine("------------------- End ----------------");

        //    Console.ReadLine();
        //}

        //---------------------------------------------------------------


        static void Main(string[] args)
        {
            Console.WriteLine("------------------- Start ----------------");

            // Read Excel Data
            Console.WriteLine("Reading records from excel started...");
            string filePath = @"D:\Temp\Akshay\ExtractDataFromExcel\ExtractExcelData\Excel\Input\ParcelData.xlsx";
            var parcelRecords = ReadParcelData(filePath);
            Console.WriteLine("Reading records from excel ends...");

            foreach (var parcelRecord in parcelRecords)
            {
                DateTime startTime = DateTime.Now;
                parcelRecord.StartTime = startTime.ToString("yyyy-MM-dd HH:mm:ss");

                try
                {
                    Console.WriteLine($"------------------- Scrapping start for parcel number - {parcelRecord.ParcelNumber}  ----------------");
                    WebScrapper(parcelRecord);
                    Console.WriteLine($"-------------------  Scrapping end for parcel number - {parcelRecord.ParcelNumber}  ----------------");

                    Console.WriteLine("Save page as pdf started...");
                    SavePageAsPdf(parcelRecord.ParcelNumber, parcelRecord.DocName).Wait();
                    Console.WriteLine("Save page as pdf ends...");

                    parcelRecord.Status = "Success";
                }
                catch (Exception ex)
                {
                    parcelRecord.Status = "Failure";
                    parcelRecord.Remark = $"System Exception: {ex.Message}";
                }
                finally
                {
                    DateTime endTime = DateTime.Now;
                    parcelRecord.EndTime = endTime.ToString("yyyy-MM-dd HH:mm:ss");
                    parcelRecord.TotalTime = (endTime - startTime).TotalSeconds.ToString("F2") + " seconds";
                }
            }

            // Save the updated parcel records back to the Excel file
            Console.WriteLine("Saving updated records to excel started...");
            UpdateParcelData(filePath, parcelRecords);
            Console.WriteLine("Saving updated records to excel ended...");

            // Save Output data to excel
            Console.WriteLine("Getting Payment Histories started...");
            var paymentHistories = GetAllPaymentHistories();
            Console.WriteLine("Getting Payment Histories ends...");

            Console.WriteLine("------------------- Saving Output data to excel started ----------------");
            SaveToExcel(paymentHistories);
            Console.WriteLine("------------------- Saving Output data to excel ended ----------------");

            Console.WriteLine("------------------- End ----------------");

            Console.ReadLine();
        }

        public static void WebScrapper(ParcelRecord parcelRecord)
        {

            // Initialize ChromeDriver
            IWebDriver driver = new ChromeDriver();

            try
            {
                // Navigate to the page
                driver.Navigate().GoToUrl("https://trweb.co.clark.nv.us");

                // Enter Parcel ID
                var parcelInput = driver.FindElement(By.XPath(@"/html/body/div[1]/center/table/tbody/tr[2]/td[2]/table[3]/tbody/tr[2]/td/table/tbody/tr[1]/td[1]/form/table/tbody/tr[1]/td[1]/table/tbody/tr/td[2]/input"));
                parcelInput.SendKeys(parcelRecord.ParcelNumber);

                // Submit the form
                var submitButton = driver.FindElement(By.Name("Submit"));
                submitButton.Click();

                // Wait for the page to load (adjust time as needed)
                System.Threading.Thread.Sleep(3000);


                // Extract the required data
                var lastPaymentAmount = driver.FindElement(By.XPath("//td[contains(text(), 'Last Payment Amount')]/following-sibling::td")).Text.Trim();
                var lastPaymentDate = driver.FindElement(By.XPath("//td[contains(text(), 'Last Payment Date')]/following-sibling::td")).Text.Trim();
                var fiscalTaxYearPayments = driver.FindElement(By.XPath("//td[contains(text(), 'Fiscal Tax Year Payments')]/following-sibling::td")).Text.Trim();
                var priorCalendarYearPayments = driver.FindElement(By.XPath("//td[contains(text(), 'Prior Calendar Year Payments')]/following-sibling::td")).Text.Trim();
                var currentCalendarYearPayments = driver.FindElement(By.XPath("//td[contains(text(), 'Current Calendar Year Payments')]/following-sibling::td")).Text.Trim();


                // Save data to SQL Server
                SaveDataToSql(parcelRecord.ParcelNumber, parcelRecord.ParNum, parcelRecord.DocName, parcelRecord.DocName1, lastPaymentAmount, lastPaymentDate, fiscalTaxYearPayments, priorCalendarYearPayments, currentCalendarYearPayments);

                Console.WriteLine("------------- Scrapping data saved to database -------------");

                // Get the page URL after navigation
                //return driver.Url;

            }
            finally
            {
                // Quit the browser
                driver.Quit();
            }

        }


        static void SaveDataToSql(string parcelNumber,string parNum,string docName,string docName1, string lastPaymentAmount, string lastPaymentDate, string fiscalTaxYearPayments, string priorCalendarYearPayments, string currentCalendarYearPayments)
        {
            string connectionString = "server=LAPTOP-S2EFS1EF\\SQLEXPRESS;database=ParcelDB;Integrated Security=true;";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "INSERT INTO PaymentHistory1 (ParcelID,ParNum,DocName,DocName1,LastPaymentAmount, LastPaymentDate, FiscalTaxYearPayments, PriorCalendarYearPayments, CurrentCalendarYearPayments) " +
                               "VALUES " +
                               "(@ParcelID," +
                               "@ParNum," +
                               "@DocName," +
                               "@DocName1," +
                               "@LastPaymentAmount," +
                               " @LastPaymentDate," +
                               " @FiscalTaxYearPayments," +
                               " @PriorCalendarYearPayments," +
                               " @CurrentCalendarYearPayments" +
                               ")";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ParcelID", parcelNumber);
                    command.Parameters.AddWithValue("@ParNum", parNum);
                    command.Parameters.AddWithValue("@DocName", docName);
                    command.Parameters.AddWithValue("@DocName1", docName1);
                    command.Parameters.AddWithValue("@LastPaymentAmount", lastPaymentAmount);
                    command.Parameters.AddWithValue("@LastPaymentDate", lastPaymentDate);
                    command.Parameters.AddWithValue("@FiscalTaxYearPayments", fiscalTaxYearPayments);
                    command.Parameters.AddWithValue("@PriorCalendarYearPayments", priorCalendarYearPayments);
                    command.Parameters.AddWithValue("@CurrentCalendarYearPayments", currentCalendarYearPayments);

                    command.ExecuteNonQuery();
                }
            }
        }



        public static List<PaymentHistory> GetAllPaymentHistories()
        {
            string connectionString = "server=LAPTOP-S2EFS1EF\\SQLEXPRESS;database=ParcelDB;Integrated Security=true;";
            var paymentHistories = new List<PaymentHistory>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT * FROM PaymentHistory1";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var paymentHistory = new PaymentHistory
                            {
                                ParcelID = reader["ParcelID"].ToString(),
                                ParNum = reader["ParNum"].ToString(),
                                DocName = reader["DocName"].ToString(),
                                DocName1 = reader["DocName"].ToString(),
                                LastPaymentAmount = reader["LastPaymentAmount"].ToString(),
                                LastPaymentDate = reader["LastPaymentDate"].ToString(),
                                FiscalTaxYearPayments = reader["FiscalTaxYearPayments"].ToString(),
                                PriorCalendarYearPayments = reader["PriorCalendarYearPayments"].ToString(),
                                CurrentCalendarYearPayments = reader["CurrentCalendarYearPayments"].ToString()
                            };

                            paymentHistories.Add(paymentHistory);
                        }
                    }
                }
            }

            return paymentHistories;
        }

        static void SaveToExcel(List<PaymentHistory> payments)
        {
            // Specify the directory where you want to save the Excel file
            string directoryPath = @"D:\Temp\Akshay\ExtractDataFromExcel\ExtractExcelData\Excel\Output\";

            // Ensure the directory exists, if not, create it
            if (!System.IO.Directory.Exists(directoryPath))
            {
                System.IO.Directory.CreateDirectory(directoryPath);
            }

            // Full path to the Excel file
            string filePath = System.IO.Path.Combine(directoryPath, "PaymentHistory.xlsx");

            // Create a new Excel workbook
            using (var workbook = new XLWorkbook())
            {
                // Add a worksheet to the workbook
                var worksheet = workbook.Worksheets.Add("PaymentHistory");

                // Add headers to the worksheet
                worksheet.Cell(1, 1).Value = "Parcel Number";
                worksheet.Cell(1, 2).Value = "ParNum";
                worksheet.Cell(1, 3).Value = "DocName";
                worksheet.Cell(1, 4).Value = "DocName1";
                worksheet.Cell(1, 5).Value = "Last Payment Amount";
                worksheet.Cell(1, 6).Value = "Last Payment Date";
                worksheet.Cell(1, 7).Value = "Fiscal Tax Year Payments";
                worksheet.Cell(1, 8).Value = "Prior Calendar Year Payments";
                worksheet.Cell(1, 9).Value = "Current Calendar Year Payments";

                // Add data to the worksheet
                for (int i = 0; i < payments.Count; i++)
                {
                    worksheet.Cell(i + 2, 1).Value = payments[i].ParcelID;
                    worksheet.Cell(i + 2, 2).Value = payments[i].ParNum;
                    worksheet.Cell(i + 2, 3).Value = payments[i].DocName;
                    worksheet.Cell(i + 2, 4).Value = payments[i].DocName1;
                    worksheet.Cell(i + 2, 5).Value = payments[i].LastPaymentAmount;
                    worksheet.Cell(i + 2, 6).Value = payments[i].LastPaymentDate; ;
                    worksheet.Cell(i + 2, 7).Value = payments[i].FiscalTaxYearPayments;
                    worksheet.Cell(i + 2, 8).Value = payments[i].PriorCalendarYearPayments;
                    worksheet.Cell(i + 2, 9).Value = payments[i].CurrentCalendarYearPayments;
                }

                // Save the workbook to the specified file path
                workbook.SaveAs(filePath);
            }

            Console.WriteLine($"Data has been successfully saved to {filePath}");
        }
        //-------------------------------------------




        // Extract data from Excel

        public static List<ParcelRecord> ReadParcelData1(string filePath)
        {
            var parcelRecords = new List<ParcelRecord>();

            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                foreach (var row in rows)
                {
                    var parcelRecord = new ParcelRecord
                    {
                        ParcelNumber = row.Cell(1).GetString(),  // A column
                        ParNum = row.Cell(2).GetString(),        // B column
                        DocName = row.Cell(3).GetString(),       // C column
                        DocName1 = row.Cell(4).GetString(),      // D column
                        Status = row.Cell(5).GetString(),          // E column
                        Remark = row.Cell(6).GetString()            // F column
                    };

                    parcelRecords.Add(parcelRecord);
                }
            }

            return parcelRecords;
        }




        public static async Task SavePageAsPdf(string parcelNumber, string docName)
        {

            string url = $"https://trweb.co.clark.nv.us/print_wep2.asp?Parcel={parcelNumber}";

            // Define the path where PDFs should be saved
            string pdfDirectory = @"D:\Temp\Akshay\ExtractDataFromExcel\ExtractExcelData\pdfs";

            // Ensure the directory exists
            if (!Directory.Exists(pdfDirectory))
            {
                Directory.CreateDirectory(pdfDirectory);
            }

            // Generate a file name based on the parcel number
            string pdfFilePath = Path.Combine(pdfDirectory, $"{docName}.pdf");

            // Set the path to your Chrome or Chromium installation
            var launchOptions = new LaunchOptions
            {
                Headless = true,
                ExecutablePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe", // Update this path accordingly
                DefaultViewport = new ViewPortOptions
                {
                    Width = 1920,
                    Height = 1080
                }
            };

            var browser = await Puppeteer.LaunchAsync(launchOptions);
            var page = await browser.NewPageAsync();

            try
            {
                // Increase timeout duration
                page.DefaultNavigationTimeout = 60000; // Set the timeout to 60 seconds

                // Navigate to the page with increased timeout
                await page.GoToAsync(url, new NavigationOptions
                {
                    Timeout = 60000, // Set the navigation timeout to 60 seconds
                    WaitUntil = new[] { WaitUntilNavigation.Networkidle2 } // Wait until network activity is idle
                });

                // Save the page as a PDF
                await page.PdfAsync(pdfFilePath, new PdfOptions
                {
                    Format = PaperFormat.A4,
                    PrintBackground = true,
                });

                Console.WriteLine($"PDF saved successfully at {pdfFilePath}!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            finally
            {
                // Dispose of resources to ensure everything is cleaned up
                if (page != null)
                {
                    await page.CloseAsync();
                }

                if (browser != null)
                {
                    await browser.CloseAsync();
                }
            }
        }


        // Update the parcel records in the Excel file
        public static void UpdateParcelData(string filePath, List<ParcelRecord> parcelRecords)
        {
            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                int rowIndex = 2;
                foreach (var row in rows)
                {
                    var parcelRecord = parcelRecords[rowIndex - 2];

                    row.Cell(5).Value = parcelRecord.Status;
                    row.Cell(6).Value = parcelRecord.Remark;    
                    row.Cell(7).Value = parcelRecord.StartTime;   
                    row.Cell(8).Value = parcelRecord.EndTime;   
                    row.Cell(9).Value = parcelRecord.TotalTime;  
                          

                    rowIndex++;
                }

                workbook.Save();
            }
        }


        public static List<ParcelRecord> ReadParcelData(string filePath)
        {
            var parcelRecords = new List<ParcelRecord>();

            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                foreach (var row in rows)
                {
                    var parcelRecord = new ParcelRecord
                    {
                        ParcelNumber = row.Cell(1).GetString(),  // A column
                        ParNum = row.Cell(2).GetString(),        // B column
                        DocName = row.Cell(3).GetString(),       // C column
                        DocName1 = row.Cell(4).GetString(),      // D column
                        Status = row.Cell(5).GetString(),        // E column
                        StartTime = row.Cell(6).GetString(),     // F column
                        EndTime = row.Cell(7).GetString(),       // G column
                        TotalTime = row.Cell(8).GetString(),     // H column
                        Remark = row.Cell(9).GetString()            // I column
                    };

                    parcelRecords.Add(parcelRecord);
                }
            }

            return parcelRecords;
        }
    }
}
