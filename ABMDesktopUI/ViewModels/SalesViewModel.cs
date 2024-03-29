﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ABMDesktopUI.Library.Api;
using ABMDesktopUI.Library.Helpers;
using ABMDesktopUI.Library.Models;
using ABMDesktopUI.Models;
using AutoMapper;
using Caliburn.Micro;

namespace ABMDesktopUI.ViewModels
{
    public class SalesViewModel : Screen
    {
        private BindingList<ProductDisplayModel> _products;
        private readonly ISaleApi _saleApi;
        private readonly IProductApi _productApi;
        private readonly IConfigHelper _configHelper;
        private readonly IMapper _mapper;
        private readonly StatusInfoViewModel _statusInfo;
        private readonly IWindowManager _window;

        public SalesViewModel(IProductApi productApi, ISaleApi saleApi, IConfigHelper configHelper, IMapper mapper, StatusInfoViewModel statusInfo, IWindowManager window)
        {
            _configHelper = configHelper;
            _mapper = mapper;
            _statusInfo = statusInfo;
            _window = window;
            _productApi = productApi;
            _saleApi = saleApi;
        }

        protected override async void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            try
            {
                await LoadProducts();
            }
            catch (Exception ex)
            {
                dynamic settings = new ExpandoObject();
                settings.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                settings.ResizeMode = ResizeMode.NoResize;
                settings.Title = "System Error";

                if(ex.Message == "Unauthorized")
                {
                    _statusInfo.UpdateMessage("Unauthorized Access", "You do not have permission to interact with the Sales Form");
                    _window.ShowDialog(_statusInfo, null, settings);
                }
                else
                {
                    _statusInfo.UpdateMessage("Fatal Exception", ex.Message);
                    _window.ShowDialog(_statusInfo, null, settings);
                }
                TryClose();
            }
        }
        private async Task LoadProducts()
        {
            var productList = await _productApi.GetAll();
            var products = _mapper.Map<List<ProductDisplayModel>>(productList);
            Products = new BindingList<ProductDisplayModel>(products);
        }
        public BindingList<ProductDisplayModel> Products
        {
            get { return _products; }
            set
            {
                _products = value;
                NotifyOfPropertyChange(() => Products);
            }
        }

        private CartProductDisplayModel _selectedCartProduct;

        public CartProductDisplayModel SelectedCartProduct
        {
            get { return _selectedCartProduct; }
            set
            {
                _selectedCartProduct = value;
                NotifyOfPropertyChange(() => SelectedCartProduct);
                NotifyOfPropertyChange(() => CanRemoveFromCart);
            }
        }

        private ProductDisplayModel _selectedProduct;

        public ProductDisplayModel SelectedProduct
        {
            get { return _selectedProduct; }
            set 
            { 
                _selectedProduct = value;
                NotifyOfPropertyChange(() => SelectedProduct);
                NotifyOfPropertyChange(() => CanAddToCart);
            }
        }

        private async Task ResetSalesViewModel() 
        {
            Cart = new BindingList<CartProductDisplayModel>();

            await LoadProducts();

            NotifyOfPropertyChange(() => SubTotal);
            NotifyOfPropertyChange(() => Tax);
            NotifyOfPropertyChange(() => Total);
            NotifyOfPropertyChange(() => CanCheckOut);
        }

        private BindingList<CartProductDisplayModel> _cart = new BindingList<CartProductDisplayModel>();

        public BindingList<CartProductDisplayModel> Cart
        {
            get { return _cart; }
            set
            {
                _cart = value;
                NotifyOfPropertyChange(() => Cart);
            }
        }


        private int _productQuantity = 1;

        public int ProductQuantity
        {
            get { return _productQuantity; }
            set
            {
                _productQuantity = value;
                NotifyOfPropertyChange(() => ProductQuantity);
                NotifyOfPropertyChange(() => CanAddToCart);
            }
        }
        public string SubTotal
        {
            get
            {
                return CalculateSubTotal().ToString("C");
            }
        }

        private decimal CalculateSubTotal()
        {
            decimal subTotal = 0;

            foreach (var product in Cart)
            {
                subTotal += product.Product.RetailPrice * product.QuantityInCart;
            }
            return subTotal;
        }

        public string Tax
        {
            get
            {
                return CalculateTax().ToString("C");
            }
        }

        private decimal CalculateTax()
        {
            decimal tax = 0;
            decimal taxRate = _configHelper.GetTaxRate();

            tax = Cart
                .Where(x => x.Product.IsTaxable)
                .Sum(x => x.Product.RetailPrice * x.QuantityInCart * taxRate);

            //foreach (var product in Cart)
            //{
            //    if (product.Product.IsTaxable)
            //    {
            //        tax += product.Product.RetailPrice * product.QuantityInCart * taxRate;
            //    }
            //}

            return tax;
        }

        public string Total
        {
            get
            {
                decimal total = CalculateSubTotal() + CalculateTax();
                return total.ToString("C");
            }
        }

        public bool CanAddToCart
        {
            get
            {
                bool output = false;

                //Make sure something is selected
                //Make sure tere is an product quantity

                if(ProductQuantity > 0 && SelectedProduct?.QuantityInStock >= ProductQuantity)
                {
                    output = true;
                }

                return output;
            }
        }
        public void AddToCart()
        {
            CartProductDisplayModel existingProduct = Cart.FirstOrDefault(x => x.Product == SelectedProduct);
            
            if(existingProduct != null)
            {
                existingProduct.QuantityInCart += ProductQuantity;
            }

            else
            {
                CartProductDisplayModel Product = new CartProductDisplayModel
                {
                    Product = SelectedProduct,
                    QuantityInCart = ProductQuantity
                };
                Cart.Add(Product);
            }

            SelectedProduct.QuantityInStock -= ProductQuantity;
            ProductQuantity = 1;
            NotifyOfPropertyChange(() => SubTotal);
            NotifyOfPropertyChange(() => Tax);
            NotifyOfPropertyChange(() => Total);
            NotifyOfPropertyChange(() => CanCheckOut);
        }

        public bool CanRemoveFromCart
        {
            get
            {
                bool output = false;

                if (SelectedCartProduct != null && SelectedCartProduct?.QuantityInCart > 0)
                {
                    output = true;
                }

                return output;
            }
        }
        public void RemoveFromCart()
        {
            SelectedCartProduct.Product.QuantityInStock += 1;

            if (SelectedCartProduct.QuantityInCart > 1)
            {
                SelectedCartProduct.QuantityInCart -= 1;
            }

            else
            {
                Cart.Remove(SelectedCartProduct);
            }

            NotifyOfPropertyChange(() => SubTotal);
            NotifyOfPropertyChange(() => Tax);
            NotifyOfPropertyChange(() => Total);
            NotifyOfPropertyChange(() => CanCheckOut);
            NotifyOfPropertyChange(() => CanAddToCart);
        }

        public bool CanCheckOut
        {
            get
            {
                bool output = false;

                if(Cart.Count > 0)
                {
                    output = true;
                }

                return output;
            }
        }

        public StatusInfoViewModel Statusinfo { get; }

        public async Task CheckOut()
        {
            //Create a SaleModel and post to the API
            SaleModel sale = new SaleModel();
            foreach(var product in Cart)
            {
                sale.SaleDetails.Add(new SaleDetailModel
                {
                    ProductId = product.Product.Id,
                    Quantity = product.QuantityInCart
                });
            }

           await _saleApi.PostSale(sale);

           await ResetSalesViewModel();
        }


    }
}
