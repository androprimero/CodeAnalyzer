//------------------------------------------------------------------------------
// <copyright file="OleDbConnectionStringBuilder.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">Microsoft</owner>
// <owner current="true" primary="false">Microsoft</owner>
//------------------------------------------------------------------------------

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Text;

namespace System.Data.OleDb {

    [DefaultProperty("Provider")]
    [RefreshPropertiesAttribute(RefreshProperties.All)]
    [System.ComponentModel.TypeConverterAttribute(typeof(OleDbConnectionStringBuilder.OleDbConnectionStringBuilderConverter))]
    public sealed class OleDbConnectionStringBuilder : DbConnectionStringBuilder {

        private enum Keywords { // specific ordering for ConnectionString output construction
//          NamedConnection,
            FileName,

            Provider,
            DataSource,
            PersistSecurityInfo,

            OleDbServices,
        }

        private static readonly string[] _validKeywords;
        private static readonly Dictionary<string,Keywords> _keywords;

        private string[] _knownKeywords;
        private Dictionary<string,OleDbPropertyInfo> _propertyInfo;

//      private string _namedConnection   = DbConnectionStringDefaults.NamedConnection;
        private string _fileName          = DbConnectionStringDefaults.FileName;

        private string _dataSource        = DbConnectionStringDefaults.DataSource;
        private string _provider          = DbConnectionStringDefaults.Provider;

        private int _oleDbServices        = DbConnectionStringDefaults.OleDbServices;

        private bool _persistSecurityInfo = DbConnectionStringDefaults.PersistSecurityInfo;

        static OleDbConnectionStringBuilder() {
            string[] validKeywords = new string[5];
            validKeywords[(int)Keywords.DataSource]          = DbConnectionStringKeywords.DataSource;
            validKeywords[(int)Keywords.FileName]            = DbConnectionStringKeywords.FileName;
//          validKeywords[(int)Keywords.NamedConnection]     = DbConnectionStringKeywords.NamedConnection;
            validKeywords[(int)Keywords.OleDbServices]       = DbConnectionStringKeywords.OleDbServices;
            validKeywords[(int)Keywords.PersistSecurityInfo] = DbConnectionStringKeywords.PersistSecurityInfo;
            validKeywords[(int)Keywords.Provider]            = DbConnectionStringKeywords.Provider;
            _validKeywords = validKeywords;

            Dictionary<string,Keywords> hash = new Dictionary<string,Keywords>(9, StringComparer.OrdinalIgnoreCase);
            hash.Add(DbConnectionStringKeywords.DataSource,                Keywords.DataSource);
            hash.Add(DbConnectionStringKeywords.FileName,                  Keywords.FileName);
//          hash.Add(DbConnectionStringKeywords.NamedConnection,           Keywords.NamedConnection);
            hash.Add(DbConnectionStringKeywords.OleDbServices,             Keywords.OleDbServices);
            hash.Add(DbConnectionStringKeywords.PersistSecurityInfo,       Keywords.PersistSecurityInfo);
            hash.Add(DbConnectionStringKeywords.Provider,                  Keywords.Provider);
            Debug.Assert(5 == hash.Count, "initial expected size is incorrect");
            _keywords = hash;
        }

        public OleDbConnectionStringBuilder() : this(null) {
            _knownKeywords = _validKeywords;
        }

        public OleDbConnectionStringBuilder(string connectionString) : base() {
            if (!ADP.IsEmpty(connectionString)) {
                ConnectionString = connectionString;
            }
        }

        public override object this[string keyword] {
            get {
                ADP.CheckArgumentNull(keyword, "keyword");
                object value;
                Keywords index;
                if (_keywords.TryGetValue(keyword, out index)) {
                    value = GetAt(index);
                }
                else if (!base.TryGetValue(keyword, out value)) {
                    Dictionary<string,OleDbPropertyInfo> dynamic = GetProviderInfo(Provider);
                    OleDbPropertyInfo info = dynamic[keyword];
                    value = info._defaultValue;
                }
                return value;
            }
            set {
                if (null != value) {
                    ADP.CheckArgumentNull(keyword, "keyword");
                    Keywords index;
                    if (_keywords.TryGetValue(keyword, out index)) {
                        switch(index) {
                        case Keywords.DataSource:          DataSource = ConvertToString(value); break;
                        case Keywords.FileName:            FileName = ConvertToString(value); break;
//                      case Keywords.NamedConnection:     NamedConnection = ConvertToString(value); break;
                        case Keywords.Provider:            Provider = ConvertToString(value); break;

                        case Keywords.OleDbServices:       OleDbServices = ConvertToInt32(value); break;

                        case Keywords.PersistSecurityInfo: PersistSecurityInfo = ConvertToBoolean(value); break;
                        default:
                            Debug.Assert(false, "unexpected keyword");
                            throw ADP.KeywordNotSupported(keyword);
                        }
                    }
                    else {
                        base[keyword] = value;
                        ClearPropertyDescriptors();
                    }
                }
                else {
                    Remove(keyword);
                }
            }
        }

        [DisplayName(DbConnectionStringKeywords.DataSource)]
        [ResCategoryAttribute(Res.DataCategory_Source)]
        [ResDescriptionAttribute(Res.DbConnectionString_DataSource)]
        [RefreshPropertiesAttribute(RefreshProperties.All)]
        // 
        public string DataSource {
            get { return _dataSource; }
            set {
                SetValue(DbConnectionStringKeywords.DataSource, value);
                _dataSource = value;
            }
        }

        [DisplayName(DbConnectionStringKeywords.FileName)]
        [ResCategoryAttribute(Res.DataCategory_NamedConnectionString)]
        [ResDescriptionAttribute(Res.DbConnectionString_FileName)]
        [RefreshPropertiesAttribute(RefreshProperties.All)]
        // 
        [Editor("System.Windows.Forms.Design.FileNameEditor, " + AssemblyRef.SystemDesign, "System.Drawing.Design.UITypeEditor, " + AssemblyRef.SystemDrawing)]
        public string FileName {
            get { return _fileName; }
            set {
                SetValue(DbConnectionStringKeywords.FileName, value);
                _fileName = value;
            }
        }

/*
        [DisplayName(DbConnectionStringKeywords.NamedConnection)]
        [ResCategoryAttribute(Res.DataCategory_NamedConnectionString)]
        [ResDescriptionAttribute(Res.DbConnectionString_NamedConnection)]
        [RefreshPropertiesAttribute(RefreshProperties.All)]
        [TypeConverter(typeof(NamedConnectionStringConverter))]
        public string NamedConnection {
            get { return _namedConnection; }
            set {
                SetValue(DbConnectionStringKeywords.NamedConnection, value);
                _namedConnection = value;
            }
        }
*/
        [DisplayName(DbConnectionStringKeywords.OleDbServices)]
        [ResCategoryAttribute(Res.DataCategory_Pooling)]
        [ResDescriptionAttribute(Res.DbConnectionString_OleDbServices)]
        [RefreshPropertiesAttribute(RefreshProperties.All)]
        [TypeConverter(typeof(OleDbConnectionStringBuilder.OleDbServicesConverter))]
        public int OleDbServices {
            get { return _oleDbServices; }
            set {
                SetValue(DbConnectionStringKeywords.OleDbServices, value);
                _oleDbServices = value;
            }
        }

        [DisplayName(DbConnectionStringKeywords.PersistSecurityInfo)]
        [ResCategoryAttribute(Res.DataCategory_Security)]
        [ResDescriptionAttribute(Res.DbConnectionString_PersistSecurityInfo)]
        [RefreshPropertiesAttribute(RefreshProperties.All)]
        public bool PersistSecurityInfo {
            get { return _persistSecurityInfo; }
            set {
                SetValue(DbConnectionStringKeywords.PersistSecurityInfo, value);
                _persistSecurityInfo = value;
            }
        }

        [DisplayName(DbConnectionStringKeywords.Provider)]
        [ResCategoryAttribute(Res.DataCategory_Source)]
        [ResDescriptionAttribute(Res.DbConnectionString_Provider)]
        [RefreshPropertiesAttribute(RefreshProperties.All)]
        [TypeConverter(typeof(OleDbConnectionStringBuilder.OleDbProviderConverter))]
        public string Provider {
            get { return _provider; }
            set {
                SetValue(DbConnectionStringKeywords.Provider, value);
                _provider = value;
                RestartProvider();
            }
        }

        public override ICollection Keys {
            get {
                string[] knownKeywords = _knownKeywords;
                if (null == knownKeywords) {

                    Dictionary<string,OleDbPropertyInfo> dynamic = GetProviderInfo(Provider);
                    if (0 < dynamic.Count) {
                        knownKeywords = new string[_validKeywords.Length + dynamic.Count];
                        _validKeywords.CopyTo(knownKeywords, 0);
                        dynamic.Keys.CopyTo(knownKeywords, _validKeywords.Length);
                    }
                    else {
                        knownKeywords = _validKeywords;
                    }

                    int count = 0;
                    foreach(string keyword in base.Keys) {
                        bool flag = true;
                        foreach(string s in knownKeywords) {
                            if (StringComparer.OrdinalIgnoreCase.Equals(s, keyword)) {
                                flag = false;
                                break;
                            }
                        }
                        if (flag) {
                            count++;
                        }
                    }
                    if (0 < count) {
                        string[] tmp = new string[knownKeywords.Length + count];
                        knownKeywords.CopyTo(tmp, 0);

                        int index = knownKeywords.Length;
                        foreach(string keyword in base.Keys) {
                            bool flag = true;
                            foreach(string s in knownKeywords) {
                                if (StringComparer.OrdinalIgnoreCase.Equals(s, keyword)) {
                                    flag = false;
                                    break;
                                }
                            }
                            if (flag) {
                                tmp[index++] = keyword;
                            }
                        }
                        knownKeywords = tmp;
                    }
                    _knownKeywords = knownKeywords;
                }
                return new System.Data.Common.ReadOnlyCollection<string>(knownKeywords);
            }
        }

        public override bool ContainsKey(string keyword) {
            ADP.CheckArgumentNull(keyword, "keyword");
            return _keywords.ContainsKey(keyword) || base.ContainsKey(keyword);
        }

        private static bool ConvertToBoolean(object value) {
            return DbConnectionStringBuilderUtil.ConvertToBoolean(value);
        }
        private static int ConvertToInt32(object value) {
            return DbConnectionStringBuilderUtil.ConvertToInt32(value);
        }
        private static string ConvertToString(object value) {
            return DbConnectionStringBuilderUtil.ConvertToString(value);
        }

        public override void Clear() {
            base.Clear();
            for(int i = 0; i < _validKeywords.Length; ++i) {
                Reset((Keywords)i);
            }
            base.ClearPropertyDescriptors();
            _knownKeywords = _validKeywords;
        }

        private object GetAt(Keywords index) {
            switch(index) {
            case Keywords.DataSource:          return DataSource;
            case Keywords.FileName:            return FileName;
//          case Keywords.NamedConnection:     return NamedConnection;
            case Keywords.OleDbServices:       return OleDbServices;
            case Keywords.PersistSecurityInfo: return PersistSecurityInfo;
            case Keywords.Provider:            return Provider;
            default:
                Debug.Assert(false, "unexpected keyword");
                throw ADP.KeywordNotSupported(_validKeywords[(int)index]);
            }
        }

        public override bool Remove(string keyword) {
            ADP.CheckArgumentNull(keyword, "keyword");
            bool value = base.Remove(keyword);

            Keywords index;
            if (_keywords.TryGetValue(keyword, out index)) {
                Reset(index);
            }
            else if (value) {
                ClearPropertyDescriptors();
            }
            return value;
        }

        private void Reset(Keywords index) {
            switch(index) {
            case Keywords.DataSource:
                _dataSource = DbConnectionStringDefaults.DataSource;
                break;
            case Keywords.FileName:
                _fileName = DbConnectionStringDefaults.FileName;
                RestartProvider();
                break;
//          case Keywords.NamedConnection:
//             _namedConnection = DbConnectionStringDefaults.NamedConnection;
//              break;
            case Keywords.OleDbServices:
                _oleDbServices = DbConnectionStringDefaults.OleDbServices;
                break;
            case Keywords.PersistSecurityInfo:
                _persistSecurityInfo = DbConnectionStringDefaults.PersistSecurityInfo;
                break;
            case Keywords.Provider:
                _provider = DbConnectionStringDefaults.Provider;
                RestartProvider();
                break;
             default:
                Debug.Assert(false, "unexpected keyword");
                throw ADP.KeywordNotSupported(_validKeywords[(int)index]);
            }
        }

        private new void ClearPropertyDescriptors() {
            base.ClearPropertyDescriptors();
            _knownKeywords = null;
        }

        private void RestartProvider() {
            ClearPropertyDescriptors();
            _propertyInfo = null;
        }

        private void SetValue(string keyword, bool value) {
            base[keyword] = value.ToString((System.IFormatProvider)null);
        }
        private void SetValue(string keyword, int value) {
            base[keyword] = value.ToString((System.IFormatProvider)null);
        }
        private void SetValue(string keyword, string value) {
            ADP.CheckArgumentNull(value, keyword);
            base[keyword] = value;
        }

        public override bool TryGetValue(string keyword, out object value) {
            ADP.CheckArgumentNull(keyword, "keyword");
            Keywords index;
            if (_keywords.TryGetValue(keyword, out index)) {
                value = GetAt(index);
                return true;
            }
            else if (!base.TryGetValue(keyword, out value)) {
                Dictionary<string,OleDbPropertyInfo> dynamic = GetProviderInfo(Provider);
                OleDbPropertyInfo info;
                if (dynamic.TryGetValue(keyword, out info)) {
                    value = info._defaultValue;
                    return true;
                }
                return false;
            }
            return true;
        }

        private Dictionary<string,OleDbPropertyInfo> GetProviderInfo(string provider) {
            Dictionary<string,OleDbPropertyInfo> providerInfo = _propertyInfo;
            if (null == providerInfo) {
                providerInfo = new Dictionary<string,OleDbPropertyInfo>(StringComparer.OrdinalIgnoreCase);
                if (!ADP.IsEmpty(provider)) {
                    Dictionary<string,OleDbPropertyInfo> hash = null;
                    try {
                        StringBuilder builder = new StringBuilder();
                        AppendKeyValuePair(builder, DbConnectionStringKeywords.Provider, provider);
                        OleDbConnectionString constr = new OleDbConnectionString(builder.ToString(), true);
                        constr.CreatePermissionSet().Demand();

                        // load provider without calling Initialize or CreateDataSource
                        using(OleDbConnectionInternal connection = new OleDbConnectionInternal(constr, (OleDbConnection)null)) {

                            // get all the init property information for the provider
                            hash = connection.GetPropertyInfo(new Guid[] { OleDbPropertySetGuid.DBInitAll });
                            foreach(KeyValuePair<string,OleDbPropertyInfo> entry in hash) {
                                Keywords index;
                                OleDbPropertyInfo info = entry.Value;
                                if (!_keywords.TryGetValue(info._description, out index)) {
                                    if ((OleDbPropertySetGuid.DBInit == info._propertySet) &&
                                            ((ODB.DBPROP_INIT_ASYNCH == info._propertyID) ||
                                             (ODB.DBPROP_INIT_HWND == info._propertyID) ||
                                             (ODB.DBPROP_INIT_PROMPT == info._propertyID))) {
                                        continue; // skip this keyword
                                    }
                                    providerInfo[info._description] = info;
                                }
                            }

                            // what are the unique propertysets?
                            List<Guid> listPropertySets= new List<Guid>();
                            foreach(KeyValuePair<string,OleDbPropertyInfo> entry in hash) {
                                OleDbPropertyInfo info = entry.Value;
                                if (!listPropertySets.Contains(info._propertySet)) {
                                    listPropertySets.Add(info._propertySet);
                                }
                            }
                            Guid[] arrayPropertySets = new Guid[listPropertySets.Count];
                            listPropertySets.CopyTo(arrayPropertySets, 0);

                            // get all the init property values for the provider
                            using(PropertyIDSet propidset = new PropertyIDSet(arrayPropertySets)) {
                                using(IDBPropertiesWrapper idbProperties = connection.IDBProperties()) {

                                    OleDbHResult hr;
                                    using(DBPropSet propset = new DBPropSet(idbProperties.Value, propidset, out hr)) {
                                        // VSDD 671375: OleDbConnectionStringBuilder is ignoring/hiding potential errors of OLEDB provider when reading its properties information
                                        if (0 <= (int)hr) {
                                            int count = propset.PropertySetCount;
                                            for(int i = 0; i < count; ++i) {
                                                Guid propertyset;
                                                tagDBPROP[] props = propset.GetPropertySet(i, out propertyset);

                                                // attach the default property value to the property info
                                                foreach(tagDBPROP prop in props) {

                                                    foreach(KeyValuePair<string,OleDbPropertyInfo> entry in hash) {
                                                        OleDbPropertyInfo info = entry.Value;
                                                        if ((info._propertyID == prop.dwPropertyID) && (info._propertySet == propertyset)) {
                                                            info._defaultValue = prop.vValue;

                                                            if (null == info._defaultValue) {
                                                                if (typeof(string) == info._type) {
                                                                    info._defaultValue = "";
                                                                }
                                                                else if (typeof(Int32) == info._type) {
                                                                    info._defaultValue = 0;
                                                                }
                                                                else if (typeof(Boolean) == info._type) {
                                                                    info._defaultValue = false;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch(System.InvalidOperationException e) {
                        ADP.TraceExceptionWithoutRethrow(e);
                    }
                    catch(System.Data.OleDb.OleDbException e) {
                        ADP.TraceExceptionWithoutRethrow(e);
                    }
                    catch(System.Security.SecurityException e) {
                        ADP.TraceExceptionWithoutRethrow(e);
                    }
                }
                _propertyInfo = providerInfo;
            }
            return providerInfo;
        }

        protected override void GetProperties(Hashtable propertyDescriptors) {
            Dictionary<string,OleDbPropertyInfo> providerInfo = GetProviderInfo(Provider);
            if (0 < providerInfo.Count) {

                foreach(OleDbPropertyInfo info in providerInfo.Values) {
                    Keywords index;
                    if (!_keywords.TryGetValue(info._description, out index)) { // not a strongly typed property

                        bool isReadOnly = false;
                        bool refreshOnChange = false;
                        Attribute[] attributes;
                        if (OleDbPropertySetGuid.DBInit == info._propertySet) {
                            switch(info._propertyID) {
#if DEBUG
                            case ODB.DBPROP_AUTH_PERSIST_SENSITIVE_AUTHINFO:
                            case ODB.DBPROP_INIT_ASYNCH:
                            case ODB.DBPROP_INIT_DATASOURCE:
                            case ODB.DBPROP_INIT_HWND:
                            case ODB.DBPROP_INIT_OLEDBSERVICES:
                                Debug.Assert(false, "should be handled via strongly typed property");
                                goto default;
#endif
                            case ODB.DBPROP_INIT_CATALOG:
                            case ODB.DBPROP_INIT_LOCATION:
                                attributes = new Attribute[] {
                                    BrowsableAttribute.Yes,
                                    new ResCategoryAttribute(Res.DataCategory_Source),
                                    RefreshPropertiesAttribute.All,
                                };
                                break;
                            case ODB.DBPROP_INIT_TIMEOUT:
                            case ODB.DBPROP_INIT_GENERALTIMEOUT:
                                attributes = new Attribute[] {
                                    BrowsableAttribute.Yes,
                                    new ResCategoryAttribute(Res.DataCategory_Initialization),
                                    RefreshPropertiesAttribute.All,
                                };
                                break;
                            // 'Password' & 'User ID' will be readonly if 'Integrated Security' exists
                            case ODB.DBPROP_AUTH_PASSWORD:
                                attributes = new Attribute[] {
                                    BrowsableAttribute.Yes,
                                    PasswordPropertyTextAttribute.Yes,
                                    new ResCategoryAttribute(Res.DataCategory_Security),
                                    RefreshPropertiesAttribute.All,
                                };
                                isReadOnly = ContainsKey(DbConnectionStringKeywords.IntegratedSecurity);
                                refreshOnChange = true;
                                break;
                            case ODB.DBPROP_AUTH_USERID:
                                attributes = new Attribute[] {
                                    BrowsableAttribute.Yes,
                                    new ResCategoryAttribute(Res.DataCategory_Security),
                                    RefreshPropertiesAttribute.All,
                                };
                                isReadOnly = ContainsKey(DbConnectionStringKeywords.IntegratedSecurity);
                                refreshOnChange = true;
                                break;
                            case ODB.DBPROP_AUTH_CACHE_AUTHINFO:
                            case ODB.DBPROP_AUTH_ENCRYPT_PASSWORD:
                            case ODB.DBPROP_AUTH_INTEGRATED:
                            case ODB.DBPROP_AUTH_MASK_PASSWORD:
                            case ODB.DBPROP_AUTH_PERSIST_ENCRYPTED:
                                attributes = new Attribute[] {
                                    BrowsableAttribute.Yes,
                                    new ResCategoryAttribute(Res.DataCategory_Security),
                                    RefreshPropertiesAttribute.All,
                                };
                                refreshOnChange = (ODB.DBPROP_AUTH_INTEGRATED == info._propertyID);
                                break;

                            case ODB.DBPROP_INIT_BINDFLAGS:
                            case ODB.DBPROP_INIT_IMPERSONATION_LEVEL:
                            case ODB.DBPROP_INIT_LCID:
                            case ODB.DBPROP_INIT_MODE:
                            case ODB.DBPROP_INIT_PROTECTION_LEVEL:
                            case ODB.DBPROP_INIT_PROVIDERSTRING:
                            case ODB.DBPROP_INIT_LOCKOWNER:
                                attributes = new Attribute[] {
                                    BrowsableAttribute.Yes,
                                    new ResCategoryAttribute(Res.DataCategory_Advanced),
                                    RefreshPropertiesAttribute.All,
                                };
                                break;
                            default:
                                Debug.Assert(false, "new standard propertyid");
                                attributes = new Attribute[] {
                                    BrowsableAttribute.Yes,
                                    RefreshPropertiesAttribute.All,
                                };
                                break;
                            }
                        }
                        else if (info._description.EndsWith(" Provider", StringComparison.OrdinalIgnoreCase)) {
                            attributes = new Attribute[] {
                                BrowsableAttribute.Yes,
                                RefreshPropertiesAttribute.All,
                                new ResCategoryAttribute(Res.DataCategory_Source),
                                new TypeConverterAttribute(typeof(OleDbConnectionStringBuilder.OleDbProviderConverter)),
                            };
                            refreshOnChange = true;
                        }
                        else {
                            attributes = new Attribute[] {
                                BrowsableAttribute.Yes,
                                RefreshPropertiesAttribute.All,
                                new CategoryAttribute(Provider),
                            };
                        }

                        DbConnectionStringBuilderDescriptor descriptor = new DbConnectionStringBuilderDescriptor(info._description,
                            typeof(OleDbConnectionStringBuilder), info._type, isReadOnly, attributes);
                        descriptor.RefreshOnChange = refreshOnChange;

                        propertyDescriptors[info._description] = descriptor;
                    }
                    // else strongly typed property already exists, i.e. DataSource
                }
            }
            base.GetProperties(propertyDescriptors);
        }

        private sealed class OleDbProviderConverter : StringConverter {

            private const int DBSOURCETYPE_DATASOURCE_TDP = 1;
            private const int DBSOURCETYPE_DATASOURCE_MDP = 3;

            private StandardValuesCollection _standardValues;

            // converter classes should have public ctor
            public OleDbProviderConverter() {
            }

            public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
                return true;
            }

            public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) {
                return false;
            }

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
                StandardValuesCollection dataSourceNames = _standardValues;
                if (null == _standardValues) {
                    // Get the sources rowset for the SQLOLEDB enumerator
                    DataTable table = (new OleDbEnumerator()).GetElements();

                    DataColumn column2 = table.Columns["SOURCES_NAME"];
                    DataColumn column5 = table.Columns["SOURCES_TYPE"];
                    //DataColumn column4 = table.Columns["SOURCES_DESCRIPTION"];

                    System.Collections.Generic.List<string> providerNames = new System.Collections.Generic.List<string>(table.Rows.Count);
                    foreach(DataRow row in table.Rows) {
                        int sourceType = (int)row[column5];
                        if (DBSOURCETYPE_DATASOURCE_TDP == sourceType || DBSOURCETYPE_DATASOURCE_MDP == sourceType) {
                            string progid = (string)row[column2];
                            if (!OleDbConnectionString.IsMSDASQL(progid.ToLower(CultureInfo.InvariantCulture))) {
                                if (0 > providerNames.IndexOf(progid)) {
                                    providerNames.Add(progid);
                                }
                            }
                        }
                    }

                    // Create the standard values collection that contains the sources
                    dataSourceNames = new StandardValuesCollection(providerNames);
                    _standardValues = dataSourceNames;
                }
                return dataSourceNames;
            }
        }

        [Flags()] internal enum OleDbServiceValues : int {
            DisableAll              = unchecked((int)0x00000000),
            ResourcePooling         = unchecked((int)0x00000001),
            TransactionEnlistment   = unchecked((int)0x00000002),
            ClientCursor            = unchecked((int)0x00000004),
            AggregationAfterSession = unchecked((int)0x00000008),
            EnableAll               = unchecked((int)0xffffffff),
            Default                 = ~(ClientCursor | AggregationAfterSession),
        };

        internal sealed class OleDbServicesConverter : TypeConverter {

            private StandardValuesCollection _standardValues;

            // converter classes should have public ctor
            public OleDbServicesConverter() : base() {
            }

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
                // Only know how to convert from a string
                return ((typeof(string) == sourceType) || base.CanConvertFrom(context, sourceType));
            }

            public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) {
                string svalue = (value as string);
                if (null != svalue) {
                    Int32 services;
                    if(Int32.TryParse(svalue, out services)) {
                        return services;
                    }
                    else {
                        if (svalue.IndexOf(',') != -1) {
                            int convertedValue = 0;
                            string[] values = svalue.Split(new char[] {','});
                            foreach(string v in values) {
                                convertedValue |= (int)(OleDbServiceValues)Enum.Parse(typeof(OleDbServiceValues), v, true);
                            }
                            return (int)convertedValue;;
                        }
                        else {
                            return (int)(OleDbServiceValues)Enum.Parse(typeof(OleDbServiceValues), svalue, true);
                        }
                    }
                }
                return base.ConvertFrom(context, culture, value);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
                // Only know how to convert to the NetworkLibrary enumeration
                return ((typeof(string) == destinationType) || base.CanConvertTo(context, destinationType));
            }

            public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) {
                if ((typeof(string) == destinationType) && (null != value) && (typeof(Int32) == value.GetType())) {
                    return Enum.Format(typeof(OleDbServiceValues), ((OleDbServiceValues)(int)value), "G");
                }
                return base.ConvertTo(context, culture, value, destinationType);
            }

            public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
                return true;
            }

            public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) {
                return false;
            }

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
                StandardValuesCollection standardValues = _standardValues;
                if (null == standardValues) {
                    Array objValues = Enum.GetValues(typeof(OleDbServiceValues));
                    Array.Sort(objValues, 0, objValues.Length);
                    standardValues = new StandardValuesCollection(objValues);
                    _standardValues = standardValues;
                }
                return standardValues;
            }

            public override bool IsValid(ITypeDescriptorContext context, object value) {
                return true;
                //return Enum.IsDefined(type, value);
            }
        }

        sealed internal class OleDbConnectionStringBuilderConverter : ExpandableObjectConverter {

            // converter classes should have public ctor
            public OleDbConnectionStringBuilderConverter() {
            }

            override public bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
                if (typeof(System.ComponentModel.Design.Serialization.InstanceDescriptor) == destinationType) {
                    return true;
                }
                return base.CanConvertTo(context, destinationType);
            }

            override public object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
                if (destinationType == null) {
                    throw ADP.ArgumentNull("destinationType");
                }
                if (typeof(System.ComponentModel.Design.Serialization.InstanceDescriptor) == destinationType) {
                    OleDbConnectionStringBuilder obj = (value as OleDbConnectionStringBuilder);
                    if (null != obj) {
                        return ConvertToInstanceDescriptor(obj);
                    }
                }
                return base.ConvertTo(context, culture, value, destinationType);
            }

            private System.ComponentModel.Design.Serialization.InstanceDescriptor ConvertToInstanceDescriptor(OleDbConnectionStringBuilder options) {
                Type[] ctorParams = new Type[] { typeof(string) };
                object[] ctorValues = new object[] { options.ConnectionString };
                System.Reflection.ConstructorInfo ctor = typeof(OleDbConnectionStringBuilder).GetConstructor(ctorParams);
                return new System.ComponentModel.Design.Serialization.InstanceDescriptor(ctor, ctorValues);
            }
        }
    }
}
