﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Base;
using DAL_LocalDb;
using System.Collections.ObjectModel;
using Prism.Commands;
using Prism.Events;
using Orders.Events;
using Orders.CommonTypes;
using System.Collections.Specialized;
using System.ComponentModel;
using Prism.Regions;


namespace Orders.ViewModels
{
    public class CreateViewModel : ViewModelBase, INavigationAware
    {
        LocalDbContext _context;
        IEventAggregator _eventAggregator;
        Order order;

        public CreateViewModel(LocalDbContext context, IEventAggregator eventAggregator)
        {
            _context = context;
            _eventAggregator = eventAggregator;

            SelectCommand = new DelegateCommand(Select, CanSelect);
            UnselectCommand = new DelegateCommand(Unselect, CanUnselect);
            CreateOrderCommand = new DelegateCommand(CreateOrder, CanCreateOrder);

            Customers = _context.Customers.ToList();
            Employees = _context.Employees.ToList();

        }
        #region Select products for order

        #region Commands

        public DelegateCommand SelectCommand { get; set; }
        private void Select()
        {
            ProductInOrderCollection = new ObservableCollection<ProductInOrder>(_SelectedProducts.
                Select(o => new ProductInOrder
                {
                    ID = (o as Product).ProductID,
                    Name = (o as Product).ProductName,
                    Discount = 0,
                    Quantity = 1,
                    UnitPrice = (o as Product).UnitPrice.Value

                }));
            foreach (var productInOrder in ProductInOrderCollection)
            {
                (productInOrder as ProductInOrder).PropertyChanged += ProductInOrder_PropertyChanged;
            }
            ProductInOrderCollection.CollectionChanged += OnProductInOrderCollectionChanged;

            TotalSum = GetTotalSum();

            SelectedProducts.Clear();
            OrderDate = DateTime.Now.ToLongDateString();
        }
        private bool CanSelect() => SelectedProductsIsNullOrEmpty();

        #endregion

        #region Bindable properties

        private List<Product> _products;
        public List<Product> Products
        {
            get { return _products; }
            set
            {
                SetProperty(ref _products, value);
            }
        }

        private ObservableCollection<Object> _SelectedProducts;
        public ObservableCollection<Object> SelectedProducts
        {
            get { return _SelectedProducts; }
            set
            {
                SetProperty(ref _SelectedProducts, value);
                SelectCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #endregion

        #region Create order

        #region Event handlers

        private void OnProductInOrderCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var producInOrder in e.NewItems)
                {
                    (producInOrder as ProductInOrder).PropertyChanged -= ProductInOrder_PropertyChanged;
                }
            }
            TotalSum = GetTotalSum();
        }

        private void ProductInOrder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            TotalSum = GetTotalSum();
        }

        #endregion

        #region Commands

        public DelegateCommand CreateOrderCommand { get; set; }
        private void CreateOrder()
        {
            order = new Order();

            order.CustomerID = SelectedCustomer.CustomerID;
            order.EmployeeID = SelectedEmployee.EmployeeID;
            order.OrderDate = DateTime.Parse(OrderDate);

            using (var contextTransaction = _context.Database.BeginTransaction())
            {
                try
                {
                    _context.Orders.Add(order);
                    int result = _context.SaveChanges();

                    //if (result > 0) OrderID = order.OrderID;

                    _context.Order_Details.AddRange(
                        new List<Order_Details>(
                            ProductInOrderCollection.Select(p => new Order_Details
                            {
                                OrderID = order.OrderID,
                                ProductID = p.ID,
                                UnitPrice = p.UnitPrice,
                                Quantity = p.Quantity,
                                Discount = p.Discount
                            })));

                    _context.SaveChanges();

                    contextTransaction.Commit();
                }
                catch (Exception)
                {
                    contextTransaction.Rollback();
                }
            }

            _eventAggregator.GetEvent<NewOrderCreated>().Publish(order.OrderID);

            ProductInOrderCollection = null;
            //OrderID = null;
            OrderDate = String.Empty;
            SelectedCustomer = null;
            SelectedEmployee = null;
            TotalSum = string.Empty;

            CreateOrderCommand.RaiseCanExecuteChanged();
            UnselectCommand.RaiseCanExecuteChanged();

        }
        private bool CanCreateOrder()
        {
            return (SelectedEmployee == null || SelectedCustomer == null) ? false : true;
        }

        public DelegateCommand UnselectCommand { get; set; }
        private void Unselect()
        {
            ProductInOrderCollection = null;
            OrderDate = String.Empty;
            SelectedCustomer = null;
            SelectedEmployee = null;
            TotalSum = String.Empty;

            CreateOrderCommand.RaiseCanExecuteChanged();
            UnselectCommand.RaiseCanExecuteChanged();
        }
        private bool CanUnselect() => ProductsInOrderIsNullOrEmpty();

        #endregion

        #region Bindable properties

        private ObservableCollection<ProductInOrder> _ProductInOrderCollection;
        public ObservableCollection<ProductInOrder> ProductInOrderCollection
        {
            get => _ProductInOrderCollection;
            set
            {
                SetProperty(ref _ProductInOrderCollection, value);
                UnselectCommand.RaiseCanExecuteChanged();
                CreateOrderCommand.RaiseCanExecuteChanged();
            }
        }

        public List<Employee> Employees { get; set; }

        Employee selectedEmployee;
        public Employee SelectedEmployee
        {
            get => selectedEmployee;
            set
            {
                SetProperty(ref selectedEmployee, value);
                CreateOrderCommand.RaiseCanExecuteChanged();
            }
        }

        public List<Customer> Customers { get; set; }

        Customer selectedCustomer;
        public Customer SelectedCustomer
        {
            get => selectedCustomer;
            set
            {
                SetProperty(ref selectedCustomer, value);
                CreateOrderCommand.RaiseCanExecuteChanged();
            }
        }

        private string orderDate;
        public string OrderDate
        {
            get { return orderDate; }
            set { SetProperty(ref orderDate, value); }
        }

        string customerID;
        public string CustomerID
        {
            get => customerID;
            set
            {
                SetProperty(ref customerID, value);
                order.CustomerID = customerID;
            }
        }

        private string totalSum;
        public string TotalSum
        {
            set { SetProperty(ref totalSum, value); }
            get { return totalSum; }
        }

        #endregion

        #region Screen objects


        //public class ProductInOrder:INotifyPropertyChanged
        //{
        //    public int ID { get; set; }
        //    public string Name { get; set; }
        //    public decimal UnitPrice { get; set; }
        //    public int Quantity { get; set; }
        //    public float Discount { get; set; }

        //    public event PropertyChangedEventHandler PropertyChanged;
        //}

        #endregion

        #endregion

        #region Utilites
        private bool ProductsInOrderIsNullOrEmpty()
        {
            if (ProductInOrderCollection != null)
                if (ProductInOrderCollection.Count != 0)
                    return true;
            return false;
        }
        private bool SelectedProductsIsNullOrEmpty()
        {
            if (SelectedProducts != null)
                if (SelectedProducts.Count != 0)
                    return true;
            return false;
        }
        private string GetTotalSum()
        {
            return ProductInOrderCollection.Select(p => ((Double)p.UnitPrice) * p.Quantity * (1 - p.Discount)).Sum().ToString("C2");
        }
        #endregion

        #region INavigationAware implementation

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            Products = new List<Product>(_context.Products.
                 Where(p => p.Discontinued == false && p.UnitsInStock > 0));

            Customers = _context.Customers.ToList<Customer>();
            Employees = _context.Employees.ToList<Employee>();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return false;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        { }
        #endregion
    }
}