namespace Katzebase.TestHarness
{
    public static partial class Exporter
    {
        /// <summary>
        /// This method just exports the entire AdventureWorks2012 database into the no SQL database.
        /// </summary>
        public static void ExportAll()
        {
            (new ADORepository.dbo_AWBuildVersionRepository()).Export_dbo_AWBuildVersion();
            (new ADORepository.dbo_DatabaseLogRepository()).Export_dbo_DatabaseLog();
            (new ADORepository.dbo_ErrorLogRepository()).Export_dbo_ErrorLog();
            (new ADORepository.HumanResources_DepartmentRepository()).Export_HumanResources_Department();
            (new ADORepository.HumanResources_EmployeeRepository()).Export_HumanResources_Employee();
            (new ADORepository.HumanResources_EmployeeDepartmentHistoryRepository()).Export_HumanResources_EmployeeDepartmentHistory();
            (new ADORepository.HumanResources_EmployeePayHistoryRepository()).Export_HumanResources_EmployeePayHistory();
            (new ADORepository.HumanResources_JobCandidateRepository()).Export_HumanResources_JobCandidate();
            (new ADORepository.HumanResources_ShiftRepository()).Export_HumanResources_Shift();
            (new ADORepository.Person_AddressRepository()).Export_Person_Address();
            (new ADORepository.Person_AddressTypeRepository()).Export_Person_AddressType();
            (new ADORepository.Person_BusinessEntityRepository()).Export_Person_BusinessEntity();
            (new ADORepository.Person_BusinessEntityAddressRepository()).Export_Person_BusinessEntityAddress();
            (new ADORepository.Person_BusinessEntityContactRepository()).Export_Person_BusinessEntityContact();
            (new ADORepository.Person_ContactTypeRepository()).Export_Person_ContactType();
            (new ADORepository.Person_CountryRegionRepository()).Export_Person_CountryRegion();
            (new ADORepository.Person_EmailAddressRepository()).Export_Person_EmailAddress();
            (new ADORepository.Person_PasswordRepository()).Export_Person_Password();
            (new ADORepository.Person_PersonRepository()).Export_Person_Person();
            (new ADORepository.Person_PersonPhoneRepository()).Export_Person_PersonPhone();
            (new ADORepository.Person_PhoneNumberTypeRepository()).Export_Person_PhoneNumberType();
            (new ADORepository.Person_StateProvinceRepository()).Export_Person_StateProvince();
            (new ADORepository.Production_BillOfMaterialsRepository()).Export_Production_BillOfMaterials();
            (new ADORepository.Production_CultureRepository()).Export_Production_Culture();
            (new ADORepository.Production_DocumentRepository()).Export_Production_Document();
            (new ADORepository.Production_IllustrationRepository()).Export_Production_Illustration();
            (new ADORepository.Production_LocationRepository()).Export_Production_Location();
            (new ADORepository.Production_ProductRepository()).Export_Production_Product();
            (new ADORepository.Production_ProductCategoryRepository()).Export_Production_ProductCategory();
            (new ADORepository.Production_ProductCostHistoryRepository()).Export_Production_ProductCostHistory();
            (new ADORepository.Production_ProductDescriptionRepository()).Export_Production_ProductDescription();
            (new ADORepository.Production_ProductInventoryRepository()).Export_Production_ProductInventory();
            (new ADORepository.Production_ProductListPriceHistoryRepository()).Export_Production_ProductListPriceHistory();
            (new ADORepository.Production_ProductModelRepository()).Export_Production_ProductModel();
            (new ADORepository.Production_ProductModelIllustrationRepository()).Export_Production_ProductModelIllustration();
            (new ADORepository.Production_ProductModelProductDescriptionCultureRepository()).Export_Production_ProductModelProductDescriptionCulture();
            (new ADORepository.Production_ProductPhotoRepository()).Export_Production_ProductPhoto();
            (new ADORepository.Production_ProductProductPhotoRepository()).Export_Production_ProductProductPhoto();
            (new ADORepository.Production_ProductReviewRepository()).Export_Production_ProductReview();
            (new ADORepository.Production_ProductSubcategoryRepository()).Export_Production_ProductSubcategory();
            (new ADORepository.Production_ScrapReasonRepository()).Export_Production_ScrapReason();
            (new ADORepository.Production_TransactionHistoryRepository()).Export_Production_TransactionHistory();
            (new ADORepository.Production_TransactionHistoryArchiveRepository()).Export_Production_TransactionHistoryArchive();
            (new ADORepository.Production_UnitMeasureRepository()).Export_Production_UnitMeasure();
            (new ADORepository.Production_WorkOrderRepository()).Export_Production_WorkOrder();
            (new ADORepository.Production_WorkOrderRoutingRepository()).Export_Production_WorkOrderRouting();
            (new ADORepository.Purchasing_ProductVendorRepository()).Export_Purchasing_ProductVendor();
            (new ADORepository.Purchasing_PurchaseOrderDetailRepository()).Export_Purchasing_PurchaseOrderDetail();
            (new ADORepository.Purchasing_PurchaseOrderHeaderRepository()).Export_Purchasing_PurchaseOrderHeader();
            (new ADORepository.Purchasing_ShipMethodRepository()).Export_Purchasing_ShipMethod();
            (new ADORepository.Purchasing_VendorRepository()).Export_Purchasing_Vendor();
            (new ADORepository.Sales_CountryRegionCurrencyRepository()).Export_Sales_CountryRegionCurrency();
            (new ADORepository.Sales_CreditCardRepository()).Export_Sales_CreditCard();
            (new ADORepository.Sales_CurrencyRepository()).Export_Sales_Currency();
            (new ADORepository.Sales_CurrencyRateRepository()).Export_Sales_CurrencyRate();
            (new ADORepository.Sales_CustomerRepository()).Export_Sales_Customer();
            (new ADORepository.Sales_PersonCreditCardRepository()).Export_Sales_PersonCreditCard();
            (new ADORepository.Sales_SalesOrderDetailRepository()).Export_Sales_SalesOrderDetail();
            (new ADORepository.Sales_SalesOrderHeaderRepository()).Export_Sales_SalesOrderHeader();
            (new ADORepository.Sales_SalesOrderHeaderSalesReasonRepository()).Export_Sales_SalesOrderHeaderSalesReason();
            (new ADORepository.Sales_SalesPersonRepository()).Export_Sales_SalesPerson();
            (new ADORepository.Sales_SalesPersonQuotaHistoryRepository()).Export_Sales_SalesPersonQuotaHistory();
            (new ADORepository.Sales_SalesReasonRepository()).Export_Sales_SalesReason();
            (new ADORepository.Sales_SalesTaxRateRepository()).Export_Sales_SalesTaxRate();
            (new ADORepository.Sales_SalesTerritoryRepository()).Export_Sales_SalesTerritory();
            (new ADORepository.Sales_SalesTerritoryHistoryRepository()).Export_Sales_SalesTerritoryHistory();
            (new ADORepository.Sales_ShoppingCartItemRepository()).Export_Sales_ShoppingCartItem();
            (new ADORepository.Sales_SpecialOfferRepository()).Export_Sales_SpecialOffer();
            (new ADORepository.Sales_SpecialOfferProductRepository()).Export_Sales_SpecialOfferProduct();
            (new ADORepository.Sales_StoreRepository()).Export_Sales_Store();
        }
    }
}

