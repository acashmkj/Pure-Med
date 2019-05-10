﻿using Newtonsoft.Json;
using PureMedBlockChainApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZXing.Net.Mobile.Forms;

namespace PureMedBlockChainApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CreateNewProduct : ContentPage
    {
        private ZXingBarcodeImageView barcode;

        public CreateNewProduct()
        {
            InitializeComponent();
            AbsoluteLayout.SetLayoutFlags(LoadingIndicator, AbsoluteLayoutFlags.PositionProportional);
            AbsoluteLayout.SetLayoutBounds(LoadingIndicator, new Rectangle(0.5, 0.5, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
        }

        private async void CreateProductButton_Clicked(object sender, EventArgs e)
        {
            LoadingOverlay.IsVisible = true;
            LoadingIndicatorText.Text = "Creating Product";
            string ProductName = ProductNameEntry.Text;
            string ProductType = ProductTypeEntry.Text;
            string ProcessDone = ProcessDoneEditor.Text;
            string ProcessDoneBy = Application.Current.Properties["FullName"].ToString();
            if (string.IsNullOrEmpty(ProductName) || string.IsNullOrEmpty(ProductType) || string.IsNullOrEmpty(ProcessDone))
            {
                await DisplayAlert("Error", "Fill out all the fields", "Okay");
                LoadingOverlay.IsVisible = false;
                return;
            }
            double ProcessCost = 0;
            if (double.TryParse(ProductCostEntry.Text, out ProcessCost))
            {

            }
            else
            {
                await DisplayAlert("Error", "Product Cost can only be number", "Okay");
                LoadingOverlay.IsVisible = false;
                return;
            }
            Random random = new Random(DateTime.UtcNow.Millisecond);
            SHA256 sha256 = SHA256Managed.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(ProductName.Trim() + ProcessDone.Trim() + ProductType.Trim() + ProcessDoneBy.Trim() + DateTime.UtcNow.ToLongDateString() + random.Next().ToString().Trim()+ProductCostEntry.Text.Trim());
            byte[] hash = sha256.ComputeHash(bytes);
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                result.Append(hash[i].ToString("X2"));
            }
            string ProductID = result.ToString();
            Location location = new Location();
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Medium);
                location = await Geolocation.GetLocationAsync(request);
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                await DisplayAlert("Error", "Device doesn't support gps location", "Okay");
                LoadingOverlay.IsVisible = false;
                return;
            }
            catch (PermissionException pEx)
            {
                await DisplayAlert("Error", "Give Location Access to application", "Okay");
                LoadingOverlay.IsVisible = false;
                return;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Can't Get Location", "Okay");
                LoadingOverlay.IsVisible = false;
                return;
            }


            Transaction newTransaction = new Transaction(ProductID, ProcessDone, ProcessDoneBy, location.Latitude.ToString(), location.Longitude.ToString(), ProcessCost);
            ProductInfo newProduct = new ProductInfo()
            {
                ProductName = ProductName,
                ProductType = ProductType,
                CreationDate = JsonConvert.SerializeObject(DateTime.Now),
                ProductID = ProductID,
                ProductCreator = 0,
                ID = 0
            };
            await Task.Run(async () =>
            {
                string url = "http://PureMedblockchain.acashmkj.me/BlockChain/CreateFirstTransaction";
                HttpContent q1 = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("productInfo", JsonConvert.SerializeObject(newProduct)), new KeyValuePair<string, string>("transaction", JsonConvert.SerializeObject(newTransaction)), new KeyValuePair<string, string>("userName", Application.Current.Properties["UserName"].ToString()), new KeyValuePair<string, string>("password", Application.Current.Properties["Password"].ToString()) });
                using (var httpClient = new HttpClient())
                {
                    try
                    {
                        Task<HttpResponseMessage> getResponse = httpClient.PostAsync(url, q1);
                        HttpResponseMessage response = await getResponse;
                        if (response.IsSuccessStatusCode)
                        {
                            var myContent = await response.Content.ReadAsStringAsync();
                            if (myContent == "False")
                            {
                                Device.BeginInvokeOnMainThread(() =>
                                {
                                    var Message = "Can't create transactions at present. Try again after some time.";
                                    DisplayAlert("Error", Message, "OK");
                                    LoadingOverlay.IsVisible = false;
                                    return;
                                });
                            }
                            else
                            {
                                Device.BeginInvokeOnMainThread(async () =>
                                {
                                    barcode = new ZXingBarcodeImageView
                                    {
                                        HorizontalOptions = LayoutOptions.FillAndExpand,
                                        VerticalOptions = LayoutOptions.FillAndExpand,
                                    };
                                    barcode.BarcodeFormat = ZXing.BarcodeFormat.QR_CODE;
                                    barcode.BarcodeOptions.Width = 500;
                                    barcode.BarcodeOptions.Height = 500;
                                    barcode.BarcodeValue = ProductID.Trim();
                                    ContentView QrResult = new ContentView();
                                    QrResult.Content = barcode;
                                    MainLayout.IsVisible = false;
                                    MainLabel.Text = ProductNameEntry.Text + " - "+ ProductTypeEntry.Text;
                                    MainScrollView.Content = QrResult;
                                    MainScrollView.IsVisible = true;
                                    LoadingOverlay.IsVisible = false;
                                    var Message = "Transaction Added Successfully. Save this QrCode or print from website.";
                                    await DisplayAlert("Success", Message, "OK");
                                    return;
                                });
                            }
                        }
                        else
                        {
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                var Message = "Server Is Down. Try Again After Some Time";
                                DisplayAlert("Error", Message, "OK");
                                LoadingOverlay.IsVisible = false;
                                return;
                            });
                        }
                    }
                    catch (Exception)
                    {

                        Device.BeginInvokeOnMainThread(() =>
                        {
                            var Message = "Check Your Internet Connection and Try Again";
                            DisplayAlert("Error", Message, "OK");
                            LoadingOverlay.IsVisible = false;
                            return;
                        });
                    }
                }
            });
        }

        private async void BackButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync(true);
        }

        private void ProductNameEntry_Completed(object sender, EventArgs e)
        {
            ProductTypeEntry.Focus();
        }

        private void ProductTypeEntry_Completed(object sender, EventArgs e)
        {
            ProcessDoneEditor.Focus();
        }

        private void ProductCostEntry_Completed(object sender, EventArgs e)
        {
            CreateProductButton_Clicked(null, null);
        }

        private void ProcessDoneEditor_Completed(object sender, EventArgs e)
        {
            ProductCostEntry.Text = String.Empty;
            ProductCostEntry.Focus();
        }

    }
}