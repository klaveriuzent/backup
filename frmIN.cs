using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using PR_WEIGHTBRIDGE.Application.Control;
using PR_WEIGHTBRIDGE.Data;
using PR_WEIGHTBRIDGE.Data.Posting;
using PR_WEIGHTBRIDGE.Application.Business.Object;
using PR_WEIGHTBRIDGE.Application.Business.WeightBridge;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NdefLibrary.Ndef;
// using Sydesoft.NfcDevice;
using System.Linq;
using NPRLibStandard;
using System.IO;
using System.Configuration;

namespace PR_WEIGHTBRIDGE.Application
{
    public partial class frmIN : Form
    {

        private delegate void SetControl(TextBox pTextBox, String pString);
        #region Event

        public delegate void _delOnSaveComplete(object sender, System.EventArgs e);
        public event _delOnSaveComplete OnSaveComplete;

        public delegate void _delOnTransactionCanceled(object sender, System.EventArgs e);
        public event _delOnTransactionCanceled OnTransactionCanceled;

        public delegate void _delOnPostComplete(object sender, System.EventArgs e);
        public event _delOnPostComplete OnPostComplete;

        #endregion

        #region Variables
        private bool _isOverQuota = false;
        private bool _isOverWeight = false;
        private String _strCompcode = string.Empty;
        private String _strWbnum = string.Empty;
        private String _strYear = string.Empty;
        private EnumTransactionStatus _enumStatus;

        private String _qrESTATE = string.Empty;
        private String _qrDivisi = string.Empty;
        private String _qrRunningAccount = string.Empty;
        private String _qrClerk = string.Empty;
        private DateTime _qrRefDate;
        private bool _qrUserQrCode = false;

        //Update Request Fandy 040418
        private String _isReload = string.Empty;
        Boolean NewDataReg = false;
        //End Update Request Fandy 040418
        private int originWeight = 0;
        private decimal pengaliPersen = 0;
        private bool _qrCodeDisplay = false;
        private string tempWeightOut;
        private bool checkIsVehicleRegistered;

        //New Var for suppliers
        private DataTable vendorDataTable;

        public EnumTransactionStatus TransactionStatus
        {
            get
            {
                if (this.DataHeader.RowState == DataRowState.Detached || this.DataHeader.RowState == DataRowState.Added)
                    return EnumTransactionStatus.NEW;
                else
                {
                    if (String.Compare(this.DataHeader.STATUS, "c", true) == 0)
                        return EnumTransactionStatus.CANCELED;
                    else if (String.Compare(this.DataHeader.STATUS, "p", true) == 0)
                        return EnumTransactionStatus.POSTED;
                    else if (String.Compare(this.DataHeader.STATUS, "e", true) == 0)
                        return EnumTransactionStatus.POST_ERROR;
                    else if (String.Compare(this.DataHeader.STATUS, "f", true) == 0)
                        return EnumTransactionStatus.CORRECTION_ERROR;
                    else if (String.Compare(this.DataHeader.STATUS, "r", true) == 0)
                        return EnumTransactionStatus.CORRECTION;
                    else if (this.DataTimbang.IsNETNull())
                        return EnumTransactionStatus.ONPROGRESS;
                    else if (this.DataHeader.IsSTATUSNull() || String.IsNullOrEmpty(this.DataHeader.STATUS))
                        return EnumTransactionStatus.PENDING;
                }
                return EnumTransactionStatus.NEW;
            }
            set
            {
                this._enumStatus = value;
            }
        }
        #endregion

        private bool __isFirstLoad;
        private WB_MASTER_DataSet.DL_WB_VENDORDataTable _temp_vendor;

        // private static MyACR122U acr122u;
        public string writeToNfc; public bool writeNfcAccess;

        #region Constructors
        public frmIN(String _isReload)
        {
            //Update Request Fandy 040418
            if (_isReload == "1")
            {
                this.__isFirstLoad = false;
            }
            else
            {
                this.__isFirstLoad = true;
            }
            //End Update Request Fandy 040418
            this.ShowWeightInErrorMessage = false;

            InitializeComponent();
            this.textBox1.Enabled = false;
            this.label5.Visible = false;
            this.txbVehicleNo.Enabled = false;
            this.txbDriverName.Enabled = false;


            string CameraValidation = ConfigurationManager.AppSettings["CameraValidation"];
            if (CameraValidation == "1")
            {
                this.btnSave.Enabled = false;
            }
            else if (CameraValidation == "0")
            {
                this.btnSave.Enabled = true;
            }
            // dgvDocument.Enabled = false;
        }
        public frmIN(String pCOMPCODE, String pWBNUM, String pYEAR)
        {
            this.__isFirstLoad = true;
            this.ShowWeightInErrorMessage = false;

            InitializeComponent();
            this.textBox1.Enabled = false;
            this.label5.Visible = false;
            this.recordBtn.Enabled = false;

            //Mengambil Gambar Plat
            string rootPath = AppDomain.CurrentDomain.BaseDirectory;
            string imgFolderPath = Path.Combine(rootPath, "img");

            string fileName = $"{pWBNUM}_IN.jpg";
            string filePath = Path.Combine(imgFolderPath, fileName);

            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            this.pictureBox1.Image = Image.FromFile(filePath);
            //Mengambil Gambar Plat

            //Mengambil Data Value Plat
            GetDL_WB_CAPTURE(pWBNUM);


            this._strCompcode = pCOMPCODE;
            this._strWbnum = pWBNUM;
            this._strYear = pYEAR;
        }


        private void GetDL_WB_CAPTURE(string ticketNo)
        {
            try
            {
                // Buka koneksi jika belum terbuka
                if (this.SQLConnection.State != ConnectionState.Open)
                    this.SQLConnection.Open();

                // Buat perintah SQL untuk mendapatkan data berdasarkan TICKET_NO
                using (System.Data.SqlClient.SqlCommand sqlCommand = new System.Data.SqlClient.SqlCommand())
                {
                    sqlCommand.Connection = this.SQLConnection;
                    sqlCommand.CommandText = "SELECT NOPOL_CCTV FROM DL_WB_CAPTURE WHERE TICKET_NO = @ticketNo";
                    sqlCommand.Parameters.AddWithValue("@ticketNo", ticketNo);

                    // Eksekusi perintah dan baca hasilnya
                    using (System.Data.SqlClient.SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Ambil nilai NOPOL_CCTV dan set ke textBox1
                            string nopolCCTV = reader["NOPOL_CCTV"].ToString();
                            textBox1.Text = nopolCCTV;
                            Console.WriteLine("Data berhasil didapatkan dan ditampilkan ke textBox1");
                        }
                        else
                        {
                            Console.WriteLine("TICKET_NO tidak ditemukan");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                // Tutup koneksi jika sudah selesai
                if (this.SQLConnection.State == ConnectionState.Open)
                    this.SQLConnection.Close();
            }
        }

        public frmIN()
        {
            // TODO: Complete member initialization
            GenerateQrcode();
        }
        #endregion

        #region Properties


        private usrCtrlOtherInfoBaseIN _usrCtrOtherInfo = null;
        private usrCtrlOtherInfoBaseIN OtherInfo
        {
            get { return this._usrCtrOtherInfo; }
        }

        private Dictionary<System.Windows.Forms.Control, ErrorProvider> _dictErrorProviders = null;
        private Dictionary<System.Windows.Forms.Control, ErrorProvider> ErrorProviders
        {
            get
            {
                if (_dictErrorProviders == null)
                    _dictErrorProviders = new Dictionary<System.Windows.Forms.Control, ErrorProvider>();
                return this._dictErrorProviders;
            }
        }
        private String COMPCODE
        {
            get
            {
                if (this.DataHeader == null || string.IsNullOrEmpty(this.DataHeader.COMPCODE))
                    return PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPCompanyCode;
                return this.DataHeader.COMPCODE;
            }
            set
            {
                this._strCompcode = value;
                if (this.DataHeader != null)
                    this.DataHeader.COMPCODE = value;

                if (this.DataRegistrasi != null)
                    this.DataRegistrasi.COMPCODE = value;


                if (this.DataTimbang != null)
                    this.DataTimbang.COMPCODE = value;

                if (this.OtherInfo != null)
                    this.OtherInfo.COMPCODE = value;

                foreach (Data.WB_IN_DataSet.DL_WB_IN_DOCRow dtRowDoc in this.WB_IN_DataSet.DL_WB_IN_DOC)
                {
                    if (dtRowDoc.RowState != DataRowState.Deleted)
                    {
                        dtRowDoc.COMPCODE = value;
                    }
                }
            }
        }
        private String WBNUM
        {
            get
            {
                if (this.DataHeader == null || string.IsNullOrEmpty(this.DataHeader.WBNUM))
                    return string.Empty;
                return this.DataHeader.WBNUM;
            }
            set
            {
                this._strWbnum = value;

                if (this.DataHeader != null)
                    this.DataHeader.WBNUM = value;

                if (this.DataRegistrasi != null)
                    this.DataRegistrasi.WBNUM = value;

                if (this.DataTimbang != null)
                    this.DataTimbang.WBNUM = value;

                if (this.OtherInfo != null)
                    this.OtherInfo.WBNUM = value;
                foreach (Data.WB_IN_DataSet.DL_WB_IN_DOCRow dtRowDoc in this.WB_IN_DataSet.DL_WB_IN_DOC)
                {
                    if (dtRowDoc.RowState != DataRowState.Deleted)
                    {
                        dtRowDoc.WBNUM = value;
                    }
                }
            }
        }
        private String YEAR
        {
            get
            {
                if (this.DataHeader == null || string.IsNullOrEmpty(this.DataHeader.YEAR))
                    return DateTime.Now.ToString("yyyy");
                return this.DataHeader.YEAR;
            }
            set
            {
                this._strYear = value;

                if (this.DataHeader != null)
                    this.DataHeader.YEAR = value;

                if (this.DataRegistrasi != null)
                    this.DataRegistrasi.YEAR = value;

                if (this.DataTimbang != null)
                    this.DataTimbang.YEAR = value;

                if (this.OtherInfo != null)
                    this.OtherInfo.YEAR = value;

                foreach (Data.WB_IN_DataSet.DL_WB_IN_DOCRow dtRowDoc in this.WB_IN_DataSet.DL_WB_IN_DOC)
                {
                    if (dtRowDoc.RowState != DataRowState.Deleted)
                    {
                        dtRowDoc.YEAR = value;
                    }
                }
            }
        }

        public String REFDOC
        {
            get
            {
                StringBuilder _strBuilder = new StringBuilder();
                foreach (Data.WB_IN_DataSet.DL_WB_IN_DOCRow dtRowDoc in this.WB_IN_DataSet.DL_WB_IN_DOC)
                {
                    if (dtRowDoc.RowState != DataRowState.Deleted)
                    {
                        _strBuilder.Append(String.Format("{0}-{1}, ", dtRowDoc.REFDOC, dtRowDoc.REFLINEDOC.PadLeft(3, '0')));
                    }
                }
                if (_strBuilder.ToString().Contains(", "))
                    return _strBuilder.ToString().Remove(_strBuilder.ToString().LastIndexOf(", "), 2);
                return _strBuilder.ToString();
            }
        }

        private Data.WB_IN_DataSet.DL_WB_INRow DataHeader
        {
            get
            {
                if (this.DL_WB_INBindingSource.Current == null)
                    return null;
                return (this.DL_WB_INBindingSource.Current as DataRowView).Row as Data.WB_IN_DataSet.DL_WB_INRow;
            }
        }

        public Data.WB_IN_DataSet.DL_WB_IN_REGRow DataRegistrasi
        {
            get
            {
                if (this.DL_WB_IN_REGbindingSource.Current == null)
                    return null;
                return (this.DL_WB_IN_REGbindingSource.Current as DataRowView).Row as Data.WB_IN_DataSet.DL_WB_IN_REGRow;
            }
        }
        private Data.WB_IN_DataSet.DL_WB_IN_TIMBANGRow DataTimbang
        {
            get
            {
                if (this.DL_WB_IN_TIMBANGBindingSource.Current == null)
                    return null;
                return (this.DL_WB_IN_TIMBANGBindingSource.Current as DataRowView).Row as Data.WB_IN_DataSet.DL_WB_IN_TIMBANGRow;
            }
        }
        private System.Data.SqlClient.SqlConnection SQLConnection
        {
            get { return PR_WEIGHTBRIDGE.Application.Business.clsHelper.GlobalConnection; }
        }
        private Boolean IsNewData
        {
            get
            {
                if (this.DataRegistrasi == null)
                    return false;
                return this.DataRegistrasi.RowState == DataRowState.Added || this.DataRegistrasi.RowState == DataRowState.Detached;
            }
        }
        private Boolean ChangesMade
        {
            get
            {
                bool _boolOtherInfoChangesMade = false;
                if (this.OtherInfo != null)
                    _boolOtherInfoChangesMade = this.OtherInfo.ChangesMade;

                bool _boolDocChangesMade = false;
                foreach (Data.WB_IN_DataSet.DL_WB_IN_DOCRow dtRowDoc in this.WB_IN_DataSet.DL_WB_IN_DOC)
                {
                    if (dtRowDoc.RowState != DataRowState.Deleted)
                    {
                        if (dtRowDoc.ChangesMade)
                        {
                            _boolDocChangesMade = true;
                            break;
                        }
                    }
                    else
                        _boolDocChangesMade = true;
                }

                bool boolChangesMade = this.DataRegistrasi.ChangesMade || this.DataTimbang.ChangesMade || _boolOtherInfoChangesMade;

                return boolChangesMade || _boolDocChangesMade;// || this.WB_IN_DataSet.GetChanges() != null; 
            }
        }

        private Boolean HasError
        {
            get
            {
                string sapcompcode = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPCompanyCode.Trim();
                string sapmillcode = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode.Trim();
                this.DataRegistrasi.Validate();
                this.DataTimbang.Validate();
                foreach (Data.WB_IN_DataSet.DL_WB_IN_DOCRow dtRowDoc in this.WB_IN_DataSet.DL_WB_IN_DOC)
                    if (dtRowDoc.RowState != DataRowState.Deleted)
                        dtRowDoc.ValidateNew(sapmillcode);
                //dtRowDoc.Validate();
                if (this.OtherInfo != null)
                {
                    if (sapcompcode.Equals("8500"))
                    {
                        if (sapmillcode.Equals("8550") && !String.IsNullOrEmpty(this.txbWeightOut.Text))
                            this.OtherInfo.ValidateData();

                        if (sapmillcode.Equals("8551"))
                            this.OtherInfo.ValidateData();
                    }
                    else
                    {
                        this.OtherInfo.ValidateData();
                    }
                }
                return this.WB_IN_DataSet.DL_WB_IN_DOC.GetErrors().Length > 0 || this.DataRegistrasi.HasErrors || this.DataTimbang.HasErrors || (this.OtherInfo != null && this.OtherInfo.HasError) || this.DL_WB_IN_DOCBindingSource.Count < 1;
            }
        }

        Utility.clsTextFileLogger _logger = null;
        Utility.clsTextFileLogger Logger
        {
            get
            {
                if (_logger == null)
                    _logger = new PR_WEIGHTBRIDGE.Utility.clsTextFileLogger()
                    {
                        Directory = System.Windows.Forms.Application.StartupPath
                    };
                return _logger;
            }
        }
        #endregion

        private void SetTextBoxText(TextBox pTextBox, String pString)
        {
            if (pTextBox.DataBindings["Text"] != null)
            {
                decimal _weight = string.IsNullOrEmpty(pString) ? 0 : decimal.Parse(pString);
                pTextBox.Text = _weight.ToString();//("#,##0", System.Globalization.CultureInfo.GetCultureInfo("id-ID"));
                //pTextBox.DataBindings["Text"].WriteValue();

                if (this.DataTimbang != null && !this.DataTimbang.IsWBCODE2Null() && !String.IsNullOrEmpty(this.DataTimbang.WBCODE2))
                {
                    if (!DataTimbang.IsTIMBANG1Null())
                    {
                        decimal timbang1 = DataTimbang.IsTIMBANG1Null() ? 0 : DataTimbang.TIMBANG1;
                        decimal timbang2 = string.IsNullOrEmpty(pString) ? 0 : decimal.Parse(pString);
                        //DataTimbang.NET = Math.Abs(timbang1 - timbang2);
                        txbNet.Text = Math.Abs(timbang1 - timbang2).ToString();//("#,##0", System.Globalization.CultureInfo.GetCultureInfo("id-ID"));
                    }
                }
            }
        }

        private void DisplayErrors()
        {
            ClearErrors();
            //DISPLAY ERRORS IN REGISTRATION DATA 
            foreach (Binding binding in this.DL_WB_IN_REGbindingSource.CurrencyManager.Bindings)
            {
                String strError = this.DataRegistrasi.GetColumnError(binding.BindingMemberInfo.BindingField);
                if (!String.IsNullOrEmpty(strError))
                {
                    System.Windows.Forms.Control ctrl = binding.Control;
                    if (binding.Control.Tag != null && binding.Control is System.Windows.Forms.Control)
                        ctrl = binding.Control.Tag as System.Windows.Forms.Control;
                    if (!ErrorProviders.ContainsKey(ctrl))
                    {
                        ErrorProvider errProvider = new ErrorProvider();
                        errProvider.BlinkStyle = ErrorBlinkStyle.AlwaysBlink;
                        ErrorProviders.Add(ctrl, errProvider);
                    }
                    ErrorProviders[ctrl].SetError(ctrl, strError);
                    this.tabPageRegistration.ImageIndex = 0;
                }
            }
            //DISPLAY ERRORS IN TIMBANG DATA
            foreach (Binding binding in this.DL_WB_IN_TIMBANGBindingSource.CurrencyManager.Bindings)
            {
                String strError = this.DataTimbang.GetColumnError(binding.BindingMemberInfo.BindingField);
                if (!String.IsNullOrEmpty(strError))
                {
                    System.Windows.Forms.Control ctrl = binding.Control;
                    if (binding.Control.Tag != null && binding.Control is System.Windows.Forms.Control)
                        ctrl = binding.Control.Tag as System.Windows.Forms.Control;
                    if (!ErrorProviders.ContainsKey(ctrl))
                    {
                        ErrorProvider errProvider = new ErrorProvider();
                        errProvider.BlinkStyle = ErrorBlinkStyle.AlwaysBlink;
                        ErrorProviders.Add(ctrl, errProvider);
                    }
                    ErrorProviders[ctrl].SetError(ctrl, strError);
                }
            }
            //DISPLAY ERROS IN DOC DATA
            if (this.DL_WB_IN_DOCBindingSource.Count < 1)
            {
                if (!ErrorProviders.ContainsKey(dgvDocument))
                {
                    ErrorProvider errProvider = new ErrorProvider();
                    errProvider.BlinkStyle = ErrorBlinkStyle.AlwaysBlink;
                    ErrorProviders.Add(dgvDocument, errProvider);
                }
                ErrorProviders[dgvDocument].SetError(dgvDocument, "1 Reference Document is required at minimum.");
                this.tabPageRegistration.ImageIndex = 0;
            }
            else
            {
                if (!ErrorProviders.ContainsKey(dgvDocument))
                {
                    ErrorProvider errProvider = new ErrorProvider();
                    errProvider.BlinkStyle = ErrorBlinkStyle.AlwaysBlink;
                    ErrorProviders.Add(dgvDocument, errProvider);
                }
                ErrorProviders[dgvDocument].SetError(dgvDocument, String.Empty);
            }
            //DISPLAY ERRORS IN OTHER INFO DATA 
            if (this.OtherInfo != null && this.OtherInfo.HasError)
            {
                this.OtherInfo.DisplayErrors();
                this.tabPageOtherInfo.ImageIndex = 0;
            }
        }

        private void ClearErrors()
        {
            //CLEAR PREVIOUS ERRORS
            foreach (KeyValuePair<System.Windows.Forms.Control, ErrorProvider> kvErrorProvider in this.ErrorProviders)
                kvErrorProvider.Value.SetError(kvErrorProvider.Key, String.Empty);
            this.tabPageOtherInfo.ImageIndex = -1;
            this.tabPageRegistration.ImageIndex = -1;

            if (this.OtherInfo != null)
                this.OtherInfo.ClearErrors();
        }

        private void CancelEdit()
        {
            this.DL_WB_IN_REGbindingSource.CancelEdit();
            this.DL_WB_IN_TIMBANGBindingSource.CancelEdit();
            if (this.OtherInfo != null)
                this.OtherInfo.CancelEdit();
            this.WB_IN_DataSet.DL_WB_IN_DOC.Clear();
            this.WB_IN_DataSet.RejectChanges();
        }

        private void EndEdit()
        {
            if (!this.IsNewData)
            {
                DataEndEdit();
            }
        }

        private void DataEndEdit()
        {
            this.DL_WB_INBindingSource.EndEdit();
            this.DL_WB_IN_REGbindingSource.EndEdit();
            this.DL_WB_IN_TIMBANGBindingSource.EndEdit();
            this.DL_WB_IN_DOCBindingSource.EndEdit();
            if (this.OtherInfo != null)
                this.OtherInfo.EndEdit();
        }

        public static Decimal Round(Decimal value)
        {
            int precision = -1;
            if (precision < -4 && precision > 15)
                throw new ArgumentOutOfRangeException("precision", "Must be and integer between -4 and 15");

            if (precision >= 0) return Math.Round(value, precision);
            else
            {
                precision = (int)Math.Pow(10, Math.Abs(precision));
                value = value + (5 * precision / 10);
                return Math.Round(value - (value % precision), 0);
            }
        }

        private void Save(out string ticke_no, int flag_nopol)
        {
            ticke_no = string.Empty;
            #region YS 20110825
            bool IsOut = false;


            if (!this.DataTimbang.IsIN)
            {
                IsOut = this.DataTimbang.IsOUT;
            }

            #endregion
            if (this.SQLConnection.State != ConnectionState.Open)
                this.SQLConnection.Open();

            string _transaction_name = String.Format("_{0}_{1}", "IN_CHK_SEQ", DateTime.Now.ToString("ddMMyyhhmmss"));
            System.Data.SqlClient.SqlTransaction sqlTransaction = this.SQLConnection.BeginTransaction(_transaction_name);
            this.DL_WB_TICKET_SEQ_INTableAdapter.SetTransaction(sqlTransaction);

            DateTime transactionDate = dtPickerTransactionDate.Value;

            Nullable<int> Sequence = this.DL_WB_TICKET_SEQ_INTableAdapter.SequenceExists(PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPCompanyCode, Convert.ToString(transactionDate.Year), PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode, "NR");
            if (Sequence.HasValue && Sequence.Value == 0)
                this.DL_WB_TICKET_SEQ_INTableAdapter.Insert(PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPCompanyCode, PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode, Convert.ToString(transactionDate.Year), "NR", 0);

            sqlTransaction.Commit();
            _transaction_name = String.Format("_{0}_{1}", "IN_SAVE", DateTime.Now.ToString("ddMMyyhhmmss"));
            sqlTransaction = this.SQLConnection.BeginTransaction(_transaction_name);
            try
            {
                this.DL_WB_INTableAdapter.SetTransaction(sqlTransaction);
                this.DL_WB_IN_REGTableAdapter.SetTransaction(sqlTransaction);
                this.DL_WB_IN_DOCTableAdapter.SetTransaction(sqlTransaction);
                this.DL_WB_IN_TIMBANGTableAdapter.SetTransaction(sqlTransaction);
                this.DL_WB_TICKET_SEQ_INTableAdapter.SetTransaction(sqlTransaction);

                if (this.OtherInfo != null)
                    this.OtherInfo.SetTransaction(sqlTransaction);

                Sequence = this.DL_WB_TICKET_SEQ_INTableAdapter.GetTicketNumber(PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPCompanyCode, Convert.ToString(transactionDate.Year), PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode, "NR");
                if (this.IsNewData)
                {
                    //Validasi Tabel Konfig Berita Acara Req by Fandy & Dev by Agvin 110620
                    if (this.DataTimbang.TIMBANG1 > 0)
                    {
                        Decimal DataTimbang1Actual = this.DataTimbang.TIMBANG1;
                        Decimal PengaliPersen;

                        var brtpctTA = new Data.WB_MASTER_DataSetTableAdapters.DL_WB_IN_BRTPCTTableAdapter();
                        // brtpctTA.SetConnection(Setting.SqlConnection);
                        brtpctTA.SetConnection(PR_WEIGHTBRIDGE.Application.Business.clsHelper.GlobalConnection);
                        brtpctTA.SetTransaction(sqlTransaction);
                        var ValuePCT = brtpctTA.GetDataBy(PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode, this.DataRegistrasi.CREATEDDATE.ToString("yyyy-MM-dd"));//, this.DataRegistrasi.TRANSTYPE, this.DataRegistrasi.CREATEDDATE.ToString("yyyy-MM-dd"));

                        if (ValuePCT.Rows.Count > 0 && ValuePCT[0].PCT != null && ValuePCT[0].PCT > 0)
                        {
                            PengaliPersen = ValuePCT[0].PCT;
                            Decimal TotalPct = ((PengaliPersen * originWeight) / 100);
                            Decimal TotalPctRnd = Round(TotalPct);

                            Decimal NewDataTimbang1 = this.DataTimbang.TIMBANG1 - TotalPctRnd;
                            this.DataTimbang.TIMBANG1 = DataTimbang1Actual; //NewDataTimbang1; //setelah dikurangi 
                            this.DataTimbang.TIMBANG1ACT = this.originWeight;//bobot sebenarnya
                            this.DataTimbang.TIMBANG1NET = TotalPctRnd; //pengurang (selisih)
                            this.DataTimbang.PCT = PengaliPersen;
                        }
                        else
                        {
                            this.DataTimbang.TIMBANG1ACT = DataTimbang1Actual;
                            this.DataTimbang.TIMBANG1NET = 0;
                            this.DataTimbang.PCT = 0;
                        }
                        this.DataTimbang.WBDATE1 = DateTime.Now;
                    }
                    //End Validasi Tabel Konfig Berita Acara Req by Fandy & Dev by Agvin 110620

                    //Update Request Fandy 040418
                    NewDataReg = true;
                    //End Update Request Fandy 040418
                    this.dtPickerTransactionDate.DataBindings["Value"].WriteValue();
                    this.DL_WB_TICKET_SEQ_INTableAdapter.UpdateSequence(Sequence, this.COMPCODE, Convert.ToString(transactionDate.Year), PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode, "NR");
                    String strTicketNumber = PR_WEIGHTBRIDGE.Application.Business.clsHelper.GetFormatedTicket(PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode, "NR", this.DataRegistrasi.WBDATE, Sequence.Value);
                    this.WBNUM = strTicketNumber;
                    ticke_no = strTicketNumber;
                    //Add perubahan QR baru 250620
                    foreach (Data.WB_IN_DataSet.DL_WB_IN_DOC_ITEMRow dtRowDocItem in this.WB_IN_DataSet.DL_WB_IN_DOC_ITEM)
                    {
                        if (dtRowDocItem.RowState != DataRowState.Deleted)
                        {
                            dtRowDocItem.WBNUM = this.WBNUM;
                        }
                    }
                    //End Add perubahan QR baru 250620
                    DataEndEdit();
                }
                else
                {
                    // untuk validasi jam out, jika sudah ada data timbang out agar tidak menimpa jam out jika ada ubah note added by jerry 25-03-2021
                    if (tempWeightOut == null)// kalau udh ada isi
                    {
                        DataTimbang.WBDATE2 = DateTime.Now;
                    }
                    else if (tempWeightOut == "")// baru mau isi
                    {
                        DataTimbang.WBDATE2 = DateTime.Now;
                    }
                }

                //YS 20110116 always get current time
                //this.DataRegistrasi.WBDATE = new DateTime(this.DataRegistrasi.WBDATE.Year, this.DataRegistrasi.WBDATE.Month, this.DataRegistrasi.WBDATE.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);

                //YS 20110825 only get current time when weigh out
                if (PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.AssignPostDate && IsOut)
                {
                    //YS 20111110 auto set date
                    this.DataRegistrasi.WBDATE = new DateTime(this.DataRegistrasi.WBDATE.Year, this.DataRegistrasi.WBDATE.Month, this.DataRegistrasi.WBDATE.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                }

                if (string.Compare(this.DataHeader.STATUS, "R", true) != 0)
                {
                    //Modif version request by Fandi
                    //if (this.DataRegistrasi.WBDATE.Date == DateTime.Now.Date)
                    //{
                    //    if (this.DataRegistrasi.WBDATE.TimeOfDay < PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.LastShiftTime.TimeOfDay)
                    //        this.DataRegistrasi.POST_DATE = this.DataRegistrasi.WBDATE.Date.AddDays(-1);
                    //    else
                    //        this.DataRegistrasi.POST_DATE = this.DataRegistrasi.WBDATE.Date;
                    //}
                    //else
                    //{
                    //    if (this.DataRegistrasi.WBDATE.TimeOfDay < PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.LastShiftTime.TimeOfDay)
                    //        this.DataRegistrasi.POST_DATE = DateTime.Now.Date.AddDays(-1);
                    //    else
                    //        this.DataRegistrasi.POST_DATE = DateTime.Now.Date;
                    //}


                    //Original version
                    if (this.DataRegistrasi.WBDATE.TimeOfDay < PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.LastShiftTime.TimeOfDay)
                        this.DataRegistrasi.POST_DATE = this.DataRegistrasi.WBDATE.Date.AddDays(-1);
                    else
                        this.DataRegistrasi.POST_DATE = this.DataRegistrasi.WBDATE.Date;
                }
                this.DataRegistrasi.VHCTYPECODE = txbVehicleCode.Text;


                this.DataHeader.STATUS = string.Compare(this.DataHeader.STATUS, "p", true) == 0 && this.ChangesMade ? "R" : this.DataHeader.STATUS;

                this.DataHeader.YEAR = Convert.ToString(transactionDate.Year);

                if (string.Compare(this.DataHeader.STATUS, "R", true) == 0)
                {
                    this.DL_WB_IN_CHANGE_LOGTableAdapter.SetTransaction(sqlTransaction);
                    string[] columns_to_exclude = new string[] { "CREATEDDATE", "CREATEDBY", "LASTUPDATEDDATE", "LASTUPDATEDBY", "YEAR", "SNAME" };
                    var reg_columns_to_log = (from col in this.DataRegistrasi.Table.Columns.OfType<DataColumn>()
                                              where !columns_to_exclude.Contains(col.ColumnName.ToUpper())
                                              select col.ColumnName);
                    foreach (string column in reg_columns_to_log)
                    {
                        object original = this.DataRegistrasi[column, DataRowVersion.Original];
                        original = original.Equals(DBNull.Value) ? string.Empty : original;
                        object current = this.DataRegistrasi[column, DataRowVersion.Current];
                        current = current.Equals(DBNull.Value) ? string.Empty : current;
                        if (string.Compare(original.ToString(), current.ToString(), true) != 0)
                        {
                            PR_WEIGHTBRIDGE.Data.WB_IN_DataSet.DL_WB_IN_CHANGE_LOGRow log_row = this.WB_IN_DataSet.DL_WB_IN_CHANGE_LOG.NewDL_WB_IN_CHANGE_LOGRow();
                            log_row.COMPCODE = this.DataHeader.COMPCODE;
                            log_row.ESTATE = this.DataHeader.ESTATE;
                            log_row.WBNUM = this.DataHeader.WBNUM;
                            log_row.FIELD_NAME = column;
                            log_row.OLD_VALUE = original.ToString();
                            log_row.NEW_VALUE = current.ToString();
                            this.WB_IN_DataSet.DL_WB_IN_CHANGE_LOG.AddDL_WB_IN_CHANGE_LOGRow(log_row);
                        }
                    }
                    var timbang_columns_to_log = (from col in this.DataTimbang.Table.Columns.OfType<DataColumn>()
                                                  where !columns_to_exclude.Contains(col.ColumnName.ToUpper())
                                                  select col.ColumnName);
                    foreach (string column in timbang_columns_to_log)
                    {
                        object original = this.DataTimbang[column, DataRowVersion.Original];
                        original = original.Equals(DBNull.Value) ? string.Empty : original;
                        object current = this.DataTimbang[column, DataRowVersion.Current];
                        current = current.Equals(DBNull.Value) ? string.Empty : current;
                        if (string.Compare(original.ToString(), current.ToString(), true) != 0)
                        {
                            PR_WEIGHTBRIDGE.Data.WB_IN_DataSet.DL_WB_IN_CHANGE_LOGRow log_row = this.WB_IN_DataSet.DL_WB_IN_CHANGE_LOG.NewDL_WB_IN_CHANGE_LOGRow();
                            log_row.COMPCODE = this.DataHeader.COMPCODE;
                            log_row.ESTATE = this.DataHeader.ESTATE;
                            log_row.WBNUM = this.DataHeader.WBNUM;
                            log_row.FIELD_NAME = column;
                            log_row.OLD_VALUE = original.ToString();
                            log_row.NEW_VALUE = current.ToString();
                            this.WB_IN_DataSet.DL_WB_IN_CHANGE_LOG.AddDL_WB_IN_CHANGE_LOGRow(log_row);
                        }
                    }
                    foreach (Data.WB_IN_DataSet.DL_WB_IN_DOCRow dtRowDoc in this.WB_IN_DataSet.DL_WB_IN_DOC)
                    {
                        var doc_columns_to_log = (from col in dtRowDoc.Table.Columns.OfType<DataColumn>()
                                                  where !columns_to_exclude.Contains(col.ColumnName.ToUpper())
                                                  select col.ColumnName);
                        foreach (string column in doc_columns_to_log)
                        {
                            if (!dtRowDoc.HasVersion(DataRowVersion.Current) || !dtRowDoc.HasVersion(DataRowVersion.Original))
                                continue;

                            object original = dtRowDoc[column, DataRowVersion.Original];
                            original = original.Equals(DBNull.Value) ? string.Empty : original;
                            object current = dtRowDoc[column, DataRowVersion.Current];
                            current = current.Equals(DBNull.Value) ? string.Empty : current;
                            if (string.Compare(original.ToString(), current.ToString(), true) != 0)
                            {
                                PR_WEIGHTBRIDGE.Data.WB_IN_DataSet.DL_WB_IN_CHANGE_LOGRow log_row = this.WB_IN_DataSet.DL_WB_IN_CHANGE_LOG.NewDL_WB_IN_CHANGE_LOGRow();
                                log_row.COMPCODE = this.DataHeader.COMPCODE;
                                log_row.ESTATE = this.DataHeader.ESTATE;
                                log_row.WBNUM = this.DataHeader.WBNUM;
                                log_row.FIELD_NAME = column;
                                log_row.OLD_VALUE = original.ToString();
                                log_row.NEW_VALUE = current.ToString();
                                this.WB_IN_DataSet.DL_WB_IN_CHANGE_LOG.AddDL_WB_IN_CHANGE_LOGRow(log_row);
                            }
                        }
                    }
                    this.DL_WB_IN_CHANGE_LOGTableAdapter.Update(this.WB_IN_DataSet.DL_WB_IN_CHANGE_LOG);


                }


                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                Logger.BeginStopwatchedLog("SAVE IN", watch);

                //Menambah FLAG
                this.DataRegistrasi.NOPOL_FLAG = flag_nopol;

                this.DL_WB_INTableAdapter.Update(this.WB_IN_DataSet.DL_WB_IN);
                this.DL_WB_IN_REGTableAdapter.Update(this.WB_IN_DataSet.DL_WB_IN_REG);
                this.DL_WB_IN_DOC_ITEMTableAdapter.SetTransaction(sqlTransaction);
                this.WB_IN_DataSet.DL_WB_IN_DOC_ITEM.Constraints.Clear();
                this.DL_WB_IN_DOC_ITEMTableAdapter.Update(this.WB_IN_DataSet.DL_WB_IN_DOC_ITEM);
                this.DL_WB_IN_DOCTableAdapter.Update(this.WB_IN_DataSet);
                this.DL_WB_IN_TIMBANGTableAdapter.Update(this.WB_IN_DataSet.DL_WB_IN_TIMBANG);

                try
                {
                    if (!IsNewData && _deletedDOC.Count > 0)
                    {
                        foreach (DataRow row in _deletedDOC)
                        {
                            row.Delete();
                        }
                        this.DL_WB_IN_DOCTableAdapter.Update(_deletedDOC);
                    }
                }
                catch (Exception) { }

                if (this.OtherInfo != null)
                    this.OtherInfo.UpdateData();


                sqlTransaction.Commit();
                Logger.EndStopwatchedLog("SAVE IN", watch);
                //ClearErrors();
                //Reload();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                this.CancelEdit();
                sqlTransaction.Rollback(_transaction_name);
                throw ex;
            }
            finally
            {
                sqlTransaction.Dispose();
                if (this.SQLConnection.State == ConnectionState.Open)
                    this.SQLConnection.Close();
                //Update Request Fandy 040418
                if ((txbVehicleCode.Text) != "")
                {
                    var IntVehicleCode = Convert.ToInt32(txbVehicleCode.Text.Substring(txbVehicleCode.Text.Length - 1));
                    if (NewDataReg == true && IntVehicleCode > 1)
                    {
                        WB_IN.CloseReader();
                        WB_OUT.CloseReader();
                        this.Hide();
                        frmIN frmIN = new frmIN(_isReload = "1");
                        frmIN.ShowDialog(this);
                        this.Close();
                    }
                }
                //End Update Request Fandy 040418
            }

            //Add validasi max qty fandy / agvin 290519
            if (this.SQLConnection.State != ConnectionState.Open)
                this.SQLConnection.Open();
            bool isAutoWeighing = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.AutoWeighing;
            if (isAutoWeighing)
            {
                var vehicleTA = new Data.WB_MASTER_DataSetTableAdapters.DL_WB_VEHICLETableAdapter();
                vehicleTA.SetConnection(Setting.SqlConnection);
                var MaxQty = vehicleTA.GetMaxQty(this.DataRegistrasi.NOPOL, this.DataRegistrasi.WBDATE.ToString("yyyy-MM-dd"));

                if (MaxQty > 0 && this.DataRegistrasi.WBNUM != "")
                {
                    this.DL_WB_IN_TIMBANGTableAdapter.UpdateTimbangByMaxQty(MaxQty, this.DataRegistrasi.WBNUM);
                }
            }
            if (this.SQLConnection.State == ConnectionState.Open)
                this.SQLConnection.Close();

        }

        private void LoadHeader()
        {
            txbCompanyID.Text = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPCompanyCode;
            txbCompanyDescription.Text = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPCompanyName;
            txbLocationID.Text = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode;
            txbLocationDescription.Text = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillName;
        }

        public WBReader.WB WB_IN = new WBReader.WB();
        public WBReader.WB WB_OUT = new WBReader.WB();
        private void LoadDevices()
        {
            WB_IN.Active = WB_OUT.Active = true;
            if (this.DataTimbang != null)
            {
                if (!this.DataTimbang.IsIN)
                    WB_IN.Active = false;
                if (!this.DataTimbang.IsOUT)
                    WB_OUT.Active = false;
            }
            chkIN.Checked = !WB_IN.Active;
            chkOUT.Checked = !WB_OUT.Active;
            // {
            WB_IN.OnSuccessEvent += new WBReader.WB.OnSuccessHandler(WB_IN_OnSuccessEvent);
            WB_IN.OnFailedEvent += new WBReader.WB.OnFailedHandler(WB_IN_OnFailedEvent);
            //}
            //else if (this.DataTimbang.IsOUT)
            //{
            WB_OUT.OnSuccessEvent += new WBReader.WB.OnSuccessHandler(WB_OUT_OnSuccessEvent);
            WB_OUT.OnFailedEvent += new WBReader.WB.OnFailedHandler(WB_OUT_OnFailedEvent);
            //}

            cmbDeviceIN.DataSource = WB_IN.getDevices().ToArray();
            cmbDeviceIN.DisplayMember = "WBName";
            cmbDeviceIN.ValueMember = "ID";

            cmbDeviceOUT.DataSource = WB_OUT.getDevices().ToArray();
            cmbDeviceOUT.DisplayMember = "WBName";
            cmbDeviceOUT.ValueMember = "ID";

        }
        void WB_OUT_OnFailedEvent(object sender, WBReader.FailedEventArgument e)
        {
            System.Text.StringBuilder _builder = new StringBuilder();
            _builder.AppendLine(String.Format("[{0}]WB-OUT Failed event : ", DateTime.Now, e.SerialError));
            System.IO.File.WriteAllText("WBReader.log", _builder.ToString());
        }

        public bool ShowWeightInErrorMessage { set; get; }

        void WB_IN_OnFailedEvent(object sender, WBReader.FailedEventArgument e)
        {
            System.Text.StringBuilder _builder = new StringBuilder();
            _builder.AppendLine(String.Format("[{0}]WB-IN Failed event : ", DateTime.Now, e.ToString()));
            System.IO.File.WriteAllText("WBReader.log", _builder.ToString());
        }

        private const int DEF_WEIGHT_IN_TOLERANCE = 10;
        private bool __isCloseCurrent = false;

        void WB_IN_OnSuccessEvent(object sender, WBReader.SuccessEventArgument e)
        {
            if (this.TransactionStatus != EnumTransactionStatus.CANCELED)
            {
                originWeight = e.Weight;
                decimal weightAfterPCT = originWeight - Round((originWeight * (pengaliPersen / 100)));

                this.txbWeightIn.DataBindings["Text"].WriteValue(); // untuk memberhentikan gerakan wb in saat ada validasi weight tolerance by jerry 12-05-2022

                this.Invoke((MethodInvoker)delegate () { txbWeightInEx.Text = originWeight.ToString(); });
                this.Invoke((MethodInvoker)delegate () { this.txbWeightIn.Text = weightAfterPCT.ToString(); });

                string strtoleranceValidation = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.UseWeightInToleranceValidation;
                bool usetoleranceValidation = false;
                if (strtoleranceValidation == "1")
                {
                    usetoleranceValidation = true;
                }

                if (this.__isFirstLoad && this.IsNewData && usetoleranceValidation)
                {
                    int toleranceWeight;
                    if (PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.WeightInTolerance <= 0)
                    {
                        toleranceWeight = DEF_WEIGHT_IN_TOLERANCE;
                    }
                    else
                    {
                        toleranceWeight = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.WeightInTolerance;
                    }

                    if ((weightAfterPCT >= (toleranceWeight + 1)) || (weightAfterPCT <= (toleranceWeight + 1) * -1))
                    {
                        this.ShowWeightInErrorMessage = true;
                        this.btn_Post.Enabled = false;
                        this.btnCorrection.Enabled = false;
                        this.btnCancelTransaction.Enabled = false;
                        this.btnPrintTicket.Enabled = false;

                        if (this.InvokeRequired)
                        {
                            //perubahan error message agar bagian background tidak bisa di klik sebelum menutup error message ini by jerry 18-05-2022
                            this.__isCloseCurrent = true;
                            delDoWeightError dwe = new delDoWeightError(DoWeightError);
                            this.Invoke(dwe, new object[] { });

                            this.__isCloseCurrent = true;
                            delDoAct dlg = new delDoAct(DoAct);
                            this.Invoke(dlg, new object[] { });
                        }
                    }
                }
                this.__isFirstLoad = false;
            }
        }

        private delegate void delDoAct();
        private void DoAct()
        {
            this.btnSave.Enabled = false;
        }
        private delegate void delDoWeightError();
        private void DoWeightError()
        {
            MessageBox.Show("Weight in must 0 or in the range of tolerantion. Please close the form", "Weight not zero", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        void WB_OUT_OnSuccessEvent(object sender, WBReader.SuccessEventArgument e)
        {
            if (this.TransactionStatus != EnumTransactionStatus.CANCELED)
            {
                if (this.txbWeightOut.InvokeRequired)
                    this.txbWeightOut.BeginInvoke(new SetControl(SetTextBoxText), new object[] { txbWeightOut, e.Weight.ToString() });
                else
                    SetTextBoxText(txbWeightOut, e.Weight.ToString());
            }
        }

        private void chkIN_CheckedChanged(object sender, EventArgs e)
        {

            WB_IN.Active = !chkIN.Checked;
            if (this.DataTimbang != null)
            {
                if (!WB_IN.Active)
                {
                    //RY 20121119 
                    //Add new validation(VENDOR QUOTA)
                    //begin

                    var RolePrivilege = new Data.WB_LOGIN_DatasetTableAdapters.DL_WB_ROLE_PRIVILEGETableAdapter();
                    RolePrivilege.SetConnection(PR_WEIGHTBRIDGE.Application.Business.clsHelper.GlobalConnection);

                    string dtNetIn = dateTimePicker1.Value.ToString("yyyy/MM/dd");
                    string dtTransDate = dtPickerTransactionDate.Value.ToString("yyyy/MM/dd");

                    //Fernando, 15/12/2016 -> Validasi Over Weight
                    var WBInWeight = new Data.WB_MASTER_DataSetTableAdapters.DL_WB_IN_WEIGHTTableAdapter();
                    WBInWeight.SetConnection(PR_WEIGHTBRIDGE.Application.Business.clsHelper.GlobalConnection);

                    if (cmbTransactionType.SelectedValue != null)
                    {
                        //Add validation no vehicle, driver name, transaction type pada tombol in request by fandy - Agvin 310119

                        if (cmbSupplier.SelectedItem != null)
                        {
                            if (!String.IsNullOrEmpty(txbVehicleNo.Text))
                            {
                                if (!String.IsNullOrEmpty(txbDriverName.Text))
                                {
                                    var queryWBInWeight = WBInWeight.GetDataByEstateTranstypeDate(PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode,
                                                            cmbTransactionType.SelectedValue.ToString(), Convert.ToDateTime(dtTransDate));

                                    if (queryWBInWeight.Rows.Count > 0)
                                    {
                                        decimal maxweight = queryWBInWeight.Rows[0].ItemArray[5] != null ? Convert.ToDecimal(queryWBInWeight.Rows[0].ItemArray[5]) : 0;
                                        if (maxweight > 0 && (Convert.ToDecimal(txbWeightIn.Text) >= maxweight))
                                        {
                                            _isOverWeight = true;
                                            MessageBox.Show("Cannot save because your Weight In is over weight for transaction type [" + cmbTransactionType.Text + "]\nMaximum Weight In is [" + (Convert.ToInt64(maxweight)).ToString() + "] kg", "Error Over Weight", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            btnSave.Enabled = false;
                                        }
                                    }

                                    //Fernando, 21/05/2013, Cek AllowVendorQuota
                                    bool AllowVendorQuota = PR_WEIGHTBRIDGE.Application.Business.clsLoginInfo.Privilege.AllowEditVendorQuota;
                                    if (AllowVendorQuota && !_isOverWeight)
                                    {
                                        var AccountingPeriodTA = new Data.WB_MASTER_DataSetTableAdapters.GET_ACCOUNTING_PERIODTableAdapter();
                                        var VendorQuotaTA = new Data.WB_MASTER_DataSetTableAdapters.DL_WB_VENDOR_QUOTATableAdapter();
                                        var QueriesTA = new Data.WB_MASTER_DataSetTableAdapters.QueriesTableAdapter();
                                        var WBInPendingTA = new Data.WB_MASTER_DataSetTableAdapters.DL_WB_IN_TIMBANGTableAdapter();
                                        var VendorTA = new Data.WB_MASTER_DataSetTableAdapters.DL_WB_VENDORTableAdapter();

                                        AccountingPeriodTA.SetConnection(PR_WEIGHTBRIDGE.Application.Business.clsHelper.GlobalConnection);
                                        VendorQuotaTA.SetConnection(PR_WEIGHTBRIDGE.Application.Business.clsHelper.GlobalConnection);
                                        QueriesTA.SetConnection(PR_WEIGHTBRIDGE.Application.Business.clsHelper.GlobalConnection);
                                        WBInPendingTA.SetConnection(PR_WEIGHTBRIDGE.Application.Business.clsHelper.GlobalConnection);
                                        VendorTA.SetConnection(PR_WEIGHTBRIDGE.Application.Business.clsHelper.GlobalConnection);

                                        //Fernando, Check Vendor Quota by Transaction Type - DL_WB_VENDOR
                                        var VendorInfo = VendorTA.GetDataByCompEstateSupplier(PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPCompanyCode,
                                                            PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode,
                                                            cmbSupplier.SelectedValue.ToString()).FirstOrDefault();

                                        if (VendorInfo != null && VendorInfo.VENDOR_QUOTA)
                                        {
                                            if (Convert.ToDateTime(dtTransDate) > Convert.ToDateTime(dtNetIn))
                                                throw new ApplicationException("Transaction Date [" + dtTransDate + "]  shouldn't greater than " + " Date In [" + dtNetIn + "]");

                                            var PeriodInfo = AccountingPeriodTA.GetData(Convert.ToDateTime(dtNetIn));
                                            if (PeriodInfo.Count < 1) throw new ApplicationException("Period for " + dtPickerTransactionDate.Value.ToShortDateString() + " not found.");

                                            //Fernando, 7 Februari 2013 --> Cek Data Period Not Null
                                            if (PeriodInfo.Count > 0 && String.IsNullOrEmpty(PeriodInfo.Rows[0].ItemArray[1].ToString())) throw new ApplicationException("Period for " + dtPickerTransactionDate.Value.ToShortDateString() + " not found.");

                                            if (cmbSupplier.SelectedValue != null)
                                            {
                                                var VendorQuotaInfo = VendorQuotaTA.GetData((int)PeriodInfo.Rows[0].ItemArray[0], (int)PeriodInfo.Rows[0].ItemArray[1],
                                                    PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode,
                                                    cmbSupplier.SelectedValue.ToString());

                                                if (VendorQuotaInfo.Rows.Count > 0)
                                                {
                                                    string dtTimeStartStr = Convert.ToDateTime(PeriodInfo.Rows[0].ItemArray[2]).ToString("yyyy/MM/dd") + " 00:00:01";
                                                    string dtTimeEndStr = Convert.ToDateTime(PeriodInfo.Rows[0].ItemArray[3]).ToString("yyyy/MM/dd") + " 23:59:59";

                                                    string dtTimeStartFinal = Convert.ToDateTime(dtTimeStartStr).ToString("yyyy/MM/dd hh:mm:ss tt");
                                                    string dtTimeEndFinal = Convert.ToDateTime(dtTimeEndStr).ToString("yyyy/MM/dd hh:mm:ss tt");


                                                    var currentNet = QueriesTA.GetTotalNetBySupplierByPeriod(cmbSupplier.SelectedValue.ToString(), Convert.ToDateTime(dtTimeStartFinal), Convert.ToDateTime(dtTimeEndFinal));
                                                    decimal quotaVendor = Convert.ToDecimal(VendorQuotaInfo.Rows[0].ItemArray[5]);
                                                    var weightEstimate = QueriesTA.GetVendorQuotaWeightEstimateByEstate(PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode.ToString());
                                                    var wbInStillPending = WBInPendingTA.GetWBInPending(cmbSupplier.SelectedValue.ToString(), Convert.ToDateTime(dtTimeStartFinal), Convert.ToDateTime(dtTimeEndFinal)).ToList();

                                                    //Potong 500 jika WBIN < Estimasi FFB
                                                    decimal weightEstimateLessEstimate = Convert.ToInt64(weightEstimate);
                                                    if (Convert.ToInt64(txbWeightIn.Text) < Convert.ToInt64(weightEstimate))
                                                    {
                                                        weightEstimateLessEstimate = Convert.ToInt64(txbWeightIn.Text) - 500;
                                                    }

                                                    //GET WbIN Still Pending
                                                    decimal currWeightStillPending = 0;
                                                    foreach (var dataPending in wbInStillPending)
                                                    {
                                                        if (Convert.ToInt64(dataPending.TIMBANG1) < Convert.ToInt64(weightEstimate))
                                                        {
                                                            currWeightStillPending += Convert.ToInt64(dataPending.TIMBANG1) - 500;
                                                        }
                                                        else
                                                        {
                                                            currWeightStillPending += Convert.ToInt64(dataPending.TIMBANG1) - Convert.ToInt64(weightEstimate);
                                                        }
                                                    }

                                                    decimal currWeight = 0;
                                                    if (currWeightStillPending > 0)
                                                    {
                                                        if (Convert.ToInt64(txbWeightIn.Text) < Convert.ToInt64(weightEstimate))
                                                        {
                                                            currWeight = quotaVendor - Convert.ToInt64(currentNet) - Convert.ToInt64(weightEstimateLessEstimate) - Convert.ToInt64(currWeightStillPending);
                                                        }
                                                        else
                                                        {
                                                            currWeight = quotaVendor - Convert.ToInt64(currentNet) - (Convert.ToInt64(txbWeightIn.Text) - Convert.ToInt64(weightEstimate)) - Convert.ToInt64(currWeightStillPending);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (Convert.ToInt64(txbWeightIn.Text) < Convert.ToInt64(weightEstimate))
                                                        {
                                                            currWeight = quotaVendor - Convert.ToInt64(currentNet) - Convert.ToInt64(weightEstimateLessEstimate);
                                                        }
                                                        else
                                                        {
                                                            currWeight = quotaVendor - Convert.ToInt64(currentNet) - (Convert.ToInt64(txbWeightIn.Text) - Convert.ToInt64(weightEstimate));
                                                        }
                                                    }


                                                    string errorMessage = string.Empty;
                                                    //decimal currWeight = quotaVendor - Convert.ToInt64(currentNet) - Convert.ToInt64(weightEstimate) - Convert.ToInt64(currentWbInNotHaveNet);
                                                    if (currWeight < 0 && Convert.ToInt64(currWeightStillPending) > 0)
                                                    {
                                                        //decimal currWeightCurrent = quotaVendor - Convert.ToInt64(currentNet) - (Convert.ToInt64(totalcurrentWbInNotHaveNet) - (Convert.ToInt64(totalRowcurrentWbInNotHaveNet) * Convert.ToInt64(weightEstimate)));
                                                        decimal currWeightCurrent = quotaVendor - Convert.ToInt64(currentNet) - Convert.ToInt64(currWeightStillPending);
                                                        if (currWeightCurrent < 0 && Convert.ToInt64(currWeightStillPending) > 0)
                                                        {
                                                            //MessageBox.Show("Cannot save because supplier " + cmbSupplier.SelectedValue.ToString() + " is over quota for current period. Current total net is " + currentNet.ToString() + ". Please finish some transaction still pending that don't have net weight, the total weight still pending is " + totalcurrentWbInNotHaveNet.ToString(), "Error Supplier Quota", MessageBoxButtons.OK, MessageBoxIcon.Error);

                                                            //decimal totalGrossStillPending = (Convert.ToInt64(totalcurrentWbInNotHaveNet) - (Convert.ToInt64(totalRowcurrentWbInNotHaveNet) * Convert.ToInt64(weightEstimate)));

                                                            errorMessage += "Cannot save because supplier " + cmbSupplier.SelectedValue.ToString() + " is over quota for current period. \n";
                                                            errorMessage += "Current Total Net : " + (Convert.ToInt64(currentNet)).ToString() + "\n";
                                                            errorMessage += "Total weight still pending is : " + (Convert.ToInt64(currWeightStillPending)).ToString() + "\n";

                                                            //MessageBox.Show("Cannot save because supplier " + cmbSupplier.SelectedValue.ToString() + " is over quota for current period. /n");
                                                            MessageBox.Show(errorMessage, "Error Supplier Quota", MessageBoxButtons.OK, MessageBoxIcon.Error);

                                                            btnSave.Enabled = false;
                                                            _isOverQuota = true;
                                                        }
                                                    }
                                                    else if (currWeight < 0)
                                                    {
                                                        decimal currWeightNettCurrent = quotaVendor - Convert.ToInt64(currentNet);
                                                        if (Convert.ToInt64(currWeightNettCurrent) < 0)
                                                        {
                                                            MessageBox.Show("Cannot save because supplier " + cmbSupplier.SelectedValue.ToString() + " is over quota for current period.  Current total net for this period is " + (Convert.ToInt64(currentNet)).ToString(), "Error Supplier Quota", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                            btnSave.Enabled = false;
                                                            _isOverQuota = true;
                                                        }
                                                    }

                                                    //if (Convert.ToDecimal(VendorQuotaInfo.Rows[0].ItemArray[5]) < (Convert.ToInt64(currentNet) + Convert.ToInt64(currentWbInNotHaveNet)))
                                                    //{
                                                    //    MessageBox.Show("Cannot Save because supplier " + cmbSupplier.SelectedValue.ToString() + " is over quota. Current total net is " + currentNet.ToString(), "Error Supplier Quota", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                    //    btnSave.Enabled = false;
                                                    //    _isOverQuota = true;
                                                    //}
                                                }
                                                else
                                                {
                                                    MessageBox.Show("Please set Vendor Quota for this period", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                    btnSave.Enabled = false;
                                                    _isOverQuota = true;
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("Please Type Driver Name", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                            else
                            {
                                MessageBox.Show("Please Type Vehicle Number", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Please choose Supplier Code", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Please choose Transaction Type", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    this.txbWeightIn.DataBindings["Text"].WriteValue();
                    this.dateTimePicker1.DataBindings["Value"].WriteValue();
                }
                else
                {
                    this.DataTimbang.TIMBANG1 = 0;
                }
            }
        }

        private void chkOUT_CheckedChanged(object sender, EventArgs e)
        {
            WB_OUT.Active = !WB_OUT.Active;
            if (this.DataTimbang != null)
            {
                if (!WB_OUT.Active)
                {
                    this.txbWeightOut.DataBindings["Text"].WriteValue();
                    this.txbNet.DataBindings["Text"].WriteValue();
                    this.dateTimePicker2.DataBindings["Value"].WriteValue();
                }
                else
                {
                    this.DataTimbang.TIMBANG2 = 0;
                }
            }
        }

        private decimal getPengaliPersen()
        {
            decimal _PengaliPersen = 0;
            try
            {
                if (this.SQLConnection.State != ConnectionState.Open)
                    this.SQLConnection.Open();

                var brtpctTA = new Data.WB_MASTER_DataSetTableAdapters.DL_WB_IN_BRTPCTTableAdapter();
                brtpctTA.SetConnection(Setting.SqlConnection);

                var ValuePCT = brtpctTA.GetDataBy(PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode, DateTime.Now.ToString("yyyy-MM-dd"));// this.DataRegistrasi.CREATEDDATE.ToString("yyyy-MM-dd"));// );
                if (ValuePCT.Rows.Count > 0 && ValuePCT[0].PCT != null && ValuePCT[0].PCT > 0)
                {
                    _PengaliPersen = ValuePCT[0].PCT;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                throw ex;
            }

            return _PengaliPersen;
        }

        private void frmIN_Load(object sender, EventArgs e)
        {
            pengaliPersen = getPengaliPersen();

            try
            {
                this.panelCorrection.Visible = false;
                SetAdaptersConnection();
                LoadDevices();
                LoadMasterData();
                Reload();



                #region initialize nfc reader

                //initialisasi object dimasukan disini karena setiap kali buka form in akan menggunakan initialisasi dari function frmIN_Load. Maka untuk object" harap di create disini saja.
                //acr122u = new MyACR122U();
                //writeNfcAccess = false;
                //writeToNfc = null;

                //try
                //{

                //    acr122u.Init(true, 800, 4, 4, 200);  // NTAG213
                //    acr122u.CardInserted += Acr122u_CardInserted; //pada saat close form in harus ditambahkan acr122u.CardInserted -= Acr122u_CardInserted; jika tidak maka function akan terus nyala walaupun Form In sudah di close.
                //    acr122u.CardRemoved += Acr122u_CardRemoved; //pada saat close form in harus ditambahkan acr122u.CardRemoved -= Acr122u_CardRemoved; jika tidak maka function akan terus nyala walaupun Form In sudah di close.
                //}
                //catch (Exception)
                //{
                //    scanNfcBtn.Visible = false;
                //    //MessageBox.Show(this, "Failed to find a reader connected to the system", "No reader connected", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //}
                #endregion

                if (this.DataHeader == null)
                {
                    DL_WB_INBindingSource.AddNew();
                    DL_WB_IN_REGbindingSource.AddNew();
                    DL_WB_IN_TIMBANGBindingSource.AddNew();
                    this.COMPCODE = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPCompanyCode;
                    this.YEAR = this.YEAR;
                    cmbTransactionType.SelectedValue = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.DefaultINTransaction;
                    if (cmbTransactionType.SelectedValue != null)
                        cmbTransactionType.DataBindings["SelectedValue"].WriteValue();
                }
                else
                {
                    this.COMPCODE = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPCompanyCode;
                    this.WB_IN_DataSet.DL_WB_IN_DOC.RowDeleting += new DataRowChangeEventHandler(DL_WB_IN_DOC_RowDeleting);
                    this.WB_IN_DataSet.DL_WB_IN_DOC.TRANSTYPE = this.DataRegistrasi.TRANSTYPE;
                    this.WB_IN_DataSet.DL_WB_IN_DOC.DL_WB_IN_REGRow = this.DataRegistrasi;

                    //YS 20111110 disable datetimepicker if condition matches
                    if (this.DataTimbang != null & !this.DataTimbang.IsIN)
                    {
                        if (PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.AssignPostDate && this.DataTimbang.IsOUT)
                        {
                            this.dtPickerTransactionDate.Value = DateTime.Now;
                        }

                        //FS, 20160615
                        cmbTransactionType.Enabled = false;
                        cmbSupplier.Enabled = false;
                        cmbSupplierCode.Enabled = false;
                        cmbEstate.Enabled = false;
                        cmbEstateCode.Enabled = false;
                        txbPONum.Enabled = false;
                        txbVehicleNo.Enabled = false;
                        txbDriverName.Enabled = false;

                        for (int i = 0; i < dgvDocument.RowCount; i++)
                        {
                            dgvDocument.Rows[i].Cells[0].ReadOnly = true;
                            dgvDocument.Rows[i].Cells[1].ReadOnly = true;
                        }

                    }
                }
                LoadHeader();

                this.txbWeightIn.Tag = this.chkIN;
                this.txbWeightOut.Tag = this.chkOUT;
                this.tempWeightOut = this.txbWeightOut.Text;// untuk validasi jika sudah timbang out tapi orang mau ubah notes pada jam yang berbeda agar tidak tertimpa jam yang sudah ada, added by jerry 25-03-2021

                this.imageList.Images.Add(this.ErrorProvider.Icon);
                this.txbStatus.Text = this.TransactionStatus.ToString().ToLower();
                this.txbVehicleCode.Text = this.DataRegistrasi.VHCTYPECODE;

                SetLock(this.WBNUM);
                this.ActiveControl = txtQRCode;
                txtQRCode.Text = "";
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                throw ex;
            }
        }

        private void LoadMasterData()
        {
            this.DL_WB_VW_TRANSTYPE_INBindingSource.DataSource = Data.clsMasterData.DL_WB_TRANSTYPE_INDataTable;
            this.DL_WB_SPTABindingSource.DataSource = Data.clsMasterData.DL_WB_SPTADataTable;
            this.DL_WB_REFTYPE_INBindingSource.DataSource = Data.clsMasterData.DL_WB_REFTYPE_INDataTable;
            this.DL_WB_STORAGELOCBindingSource.DataSource = Data.clsMasterData.DL_WB_STORAGELOCDataTable;
            //Sebelumnya
            //this.DL_WB_VENDORBindingSource.DataSource = Data.clsMasterData.DL_WB_VENDORDataTable;
            //Sesudah
            vendorDataTable = Data.clsMasterData.DL_WB_VENDORDataTable;
            this.DL_WB_VENDOR_ESTATEBindingSource.DataSource = Data.clsMasterData.DL_WB_VENDOR_ESTATEDataTable;
            //this.DL_WB_FLAG_POSTBindingSource.DataSource = Data.clsMasterData.DL_WB_FLAG_POSTDataTable;
            this._temp_vendor = Data.clsMasterData.DL_WB_VENDORDataTable;
        }

        private void Reload(System.Text.StringBuilder builder)
        {
            this.DL_WB_INTableAdapter.FillByCompcodeWbnumYear(ref builder, "IN", "LOAD IN", this.WB_IN_DataSet.DL_WB_IN, this._strCompcode, this._strWbnum, this._strYear);
            this.DL_WB_IN_REGTableAdapter.FillByCompcodeWbnumYear(ref builder, "IN", "LOAD IN REG", this.WB_IN_DataSet.DL_WB_IN_REG, this._strCompcode, this._strWbnum, this._strYear);
            this.DL_WB_IN_DOCTableAdapter.FillByCompcodeWbnumYear(ref builder, "IN", "LOAD IN DOC", this.WB_IN_DataSet.DL_WB_IN_DOC, this._strCompcode, this._strWbnum, this._strYear);
            this.DL_WB_IN_TIMBANGTableAdapter.FillByCompcodeWbnumYear(ref builder, "IN", "LOAD IN TIMBANG", this.WB_IN_DataSet.DL_WB_IN_TIMBANG, this._strCompcode, this._strWbnum, this._strYear);
            if (this.OtherInfo != null)
                this.OtherInfo.Reload(builder);
        }

        private void Reload()
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            Logger.BeginStopwatchedLog("Reload IN", watch);
            this.DL_WB_INTableAdapter.FillByCompcodeWbnumYear(this.WB_IN_DataSet.DL_WB_IN, this._strCompcode, this._strWbnum, this._strYear);
            this.DL_WB_IN_REGTableAdapter.FillByCompcodeWbnumYear(this.WB_IN_DataSet.DL_WB_IN_REG, this._strCompcode, this._strWbnum, this._strYear);
            this.DL_WB_IN_DOCTableAdapter.FillByCompcodeWbnumYear(this.WB_IN_DataSet.DL_WB_IN_DOC, this._strCompcode, this._strWbnum, this._strYear);
            this.DL_WB_IN_TIMBANGTableAdapter.FillByCompcodeWbnumYear(this.WB_IN_DataSet.DL_WB_IN_TIMBANG, this._strCompcode, this._strWbnum, this._strYear);
            if (this.OtherInfo != null)
                this.OtherInfo.Reload();
            else
                cmbTransactionType_SelectedIndexChanged(cmbTransactionType, EventArgs.Empty);

            Logger.EndStopwatchedLog("Reload IN", watch);
        }

        void DL_WB_IN_DOC_RowDeleting(object sender, DataRowChangeEventArgs e)
        {
            Data.WB_IN_DataSet.DL_WB_IN_DOCRow dtRowDoc = e.Row as Data.WB_IN_DataSet.DL_WB_IN_DOCRow;
            _deletedDOC.ImportRow(dtRowDoc);
        }

        private void SetAdaptersConnection()
        {
            this.DL_WB_INTableAdapter.SetConnection(this.SQLConnection);
            this.DL_WB_IN_REGTableAdapter.SetConnection(this.SQLConnection);
            this.DL_WB_IN_DOCTableAdapter.SetConnection(this.SQLConnection);
            this.DL_WB_IN_DOC_ITEMTableAdapter.SetConnection(this.SQLConnection);
            this.DL_WB_IN_TIMBANGTableAdapter.SetConnection(this.SQLConnection);
            this.DL_WB_POST_LOGTableAdapter.SetConnection(this.SQLConnection);
            this.DL_WB_TICKET_SEQ_INTableAdapter.SetConnection(this.SQLConnection);
            this.DL_WB_IN_CHANGE_LOGTableAdapter.SetConnection(this.SQLConnection);
            this.DL_WB_CAPTURETableAdapter.SetConnection(this.SQLConnection);
        }

        #region Binding Source List Changed
        WB_IN_DataSet.DL_WB_IN_DOCDataTable _deletedDOC = new WB_IN_DataSet.DL_WB_IN_DOCDataTable();
        private void DL_WB_IN_DOCBindingSource_ListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemAdded)
            {
                this.DL_WB_IN_DOCBindingSource.Position = e.NewIndex;
                Data.WB_IN_DataSet.DL_WB_IN_DOCRow dtRowDoc = (this.DL_WB_IN_DOCBindingSource.Current as DataRowView).Row as Data.WB_IN_DataSet.DL_WB_IN_DOCRow;
                if (dtRowDoc.RowState == DataRowState.Detached)
                {
                    dtRowDoc.COMPCODE = this.COMPCODE;
                    dtRowDoc.WBNUM = this.WBNUM;
                    dtRowDoc.YEAR = this.YEAR;

                    //Add perubahan QR baru 250620
                    if (isScanned)
                    {
                        dtRowDoc.RUNNINGACCOUNT = this._qrRunningAccount;
                        dtRowDoc.ESTATE = this._qrESTATE;
                        dtRowDoc.CLERK = this._qrClerk;
                        dtRowDoc.DIVISI = this._qrDivisi;
                        dtRowDoc.REFDATE = this._qrRefDate;
                        dtRowDoc.USEQRCODE = this._qrUserQrCode;
                    }
                    //End Add perubahan QR baru 250620
                }

                if (cmbSupplier.SelectedItem != null && string.IsNullOrEmpty(dtRowDoc.REFDOC))
                {
                    DataRowView dtRowViewTransType = cmbTransactionType.SelectedItem as DataRowView;
                    PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSet.DL_WB_TRANSTYPE_INRow dtRowTransType = dtRowViewTransType.Row as PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSet.DL_WB_TRANSTYPE_INRow;
                    if (dtRowTransType.VENDOR_DOC_FORMAT)
                    {
                        DataRowView dtRowViewSupplier = DL_WB_VENDORBindingSource.Current as DataRowView;
                        PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSet.DL_WB_VENDORRow dtRowSupplier = dtRowViewSupplier.Row as PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSet.DL_WB_VENDORRow;
                        dtRowDoc.REFDOC = dtRowSupplier.IsDOC_FORMATNull() || string.IsNullOrEmpty(dtRowSupplier.DOC_FORMAT.Trim()) ? string.Empty : DateTime.Now.ToString(dtRowSupplier.DOC_FORMAT.Trim());// dtRowSupplier.DOC_FORMAT;
                    }
                }
            }
        }
        #endregion

        private void cmbTransactionType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbTransactionType.SelectedItem != null)
            {


                var dtRowViewTransType = cmbTransactionType.SelectedItem as DataRowView;
                var dtRowTransType = dtRowViewTransType.Row as PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSet.DL_WB_TRANSTYPE_INRow;

                BindSupplier(dtRowTransType);
                FilterSLOC(dtRowTransType);
                SetupReferenceDocument(dtRowTransType);
                SetPanelVisibility(dtRowTransType);
                SetupOtherInfoControl(dtRowTransType);
            }

            void BindSupplier(PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSet.DL_WB_TRANSTYPE_INRow dtRowTransType)
            {
                var supplierData = this._temp_vendor.Where(x => x.TRX_TYPE == cmbTransactionType.SelectedValue.ToString());
                var bb = this.WB_IN_DataSet.DL_WB_IN_REG.Rows;
                if (bb.Count != 0)
                {
                    var a = this.WB_IN_DataSet.DL_WB_IN_REG.First();
                    supplierData = supplierData.Where(o => o.SUPPLIER == a.SUPPLIER);
                }
                if (supplierData.Count() > 0)
                {
                    this.DL_WB_VENDORBindingSource.DataSource = supplierData;
                }
                else
                {
                    this.DL_WB_VENDORBindingSource.DataSource = this._temp_vendor;
                }

            }

            void FilterSLOC(PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSet.DL_WB_TRANSTYPE_INRow dtRowTransType)
            {
                this.DL_WB_STORAGELOCBindingSource.DataSource = Data.clsMasterData.DL_WB_STORAGELOCDataTable;
                var mapTransType = Data.clsMasterData.DL_WB_MAP_TRANSTYPE_IN_TO_STORAGELOCDataTable.AsEnumerable()
                    .Where(record => string.Compare(record.Field<string>("TRANSTYPE").Trim(), dtRowTransType.TRANSTYPE.Trim(), true) == 0);

                if (mapTransType.Any())
                {
                    var filter = new StringBuilder();
                    foreach (var map in mapTransType)
                    {
                        filter.Append($" SLOC = '{map.Field<string>("SLOC")}' OR");
                    }
                    filter.Remove(filter.Length - 2, 2);
                    DL_WB_STORAGELOCBindingSource.Filter = filter.ToString();

                    var joinData = Data.clsMasterData.DL_WB_STORAGELOCDataTable.AsEnumerable()
                        .Join(mapTransType, a => a.Field<string>("SLOC"), b => b.Field<string>("SLOC"), (a, b) => a);
                    cmbStorageLocation.Text = joinData.FirstOrDefault()?.Field<string>("DESCRIPTION");
                }
                else
                {
                    DL_WB_STORAGELOCBindingSource.RemoveFilter();
                    cmbStorageLocation.SelectedIndex = -1;
                }
            }

            void SetupReferenceDocument(PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSet.DL_WB_TRANSTYPE_INRow dtRowTransType)
            {
                string millCode = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode;
                if (cmbTransactionType.SelectedValue.ToString().Equals("TEB1") && millCode.Equals("8551"))
                {
                    cmbReferenceDocument.SelectedValue = 1;
                }
                else
                {
                    cmbReferenceDocument.SelectedValue = dtRowTransType.REFTYPE;
                }
                this.WB_IN_DataSet.DL_WB_IN_DOC.TRANSTYPE = dtRowTransType.TRANSTYPE;
            }

            void SetPanelVisibility(PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSet.DL_WB_TRANSTYPE_INRow dtRowTransType)
            {
                string millCode = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode;

                this.panelEstate.Visible = dtRowTransType.ESTATE_ON;
                if (this.DataRegistrasi != null && this.cmbEstate.SelectedValue != null)
                {
                    this.DataRegistrasi.ESTATE = dtRowTransType.ESTATE_ON ? this.cmbEstate.SelectedValue.ToString() : string.Empty;
                }

                var panelEnabled = this.TransactionStatus != EnumTransactionStatus.POSTED && this.TransactionStatus != EnumTransactionStatus.CANCELED;
                this.panelSupplier.Enabled = panelEnabled;
                this.panelEstate.Enabled = panelEnabled;
                this.dgvDocument.Enabled = panelEnabled;
                this.dgvDocument.AllowUserToAddRows = panelEnabled;

                if (dtRowTransType.REFTYPE == 1)
                {
                    this.panelSPTA.Visible = millCode.Equals("8550") || millCode.Equals("8551");
                }
            }

            void SetupOtherInfoControl(PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSet.DL_WB_TRANSTYPE_INRow dtRowTransType)
            {
                panelOtherInfo.Controls.Clear();
                if (OtherInfo != null)
                {
                    OtherInfo.CancelEdit();
                    OtherInfo.Dispose();
                    _usrCtrOtherInfo = null;
                }

                switch (dtRowTransType.REFTYPE)
                {
                    case 0:
                        _usrCtrOtherInfo = new usrCtrlOtherInfoTBS(this.WB_IN_DataSet, this.COMPCODE, this.WBNUM, this.YEAR, cmbSupplierCode, txbLocationID.Text, dtPickerTransactionDate.Value.Date, txbVehicleNo.Text);
                        break;
                    case 1:
                        _usrCtrOtherInfo = new usrCtrlOtherInfoSugarCaneNEW(this.WB_IN_DataSet, this.COMPCODE, this.WBNUM, this.YEAR);
                        break;
                    case 2:
                        _usrCtrOtherInfo = new usrCtrlOtherInfoRubber(this.WB_IN_DataSet, this.COMPCODE, this.WBNUM, this.YEAR);
                        break;
                    case 6:
                        string lblPotonganPlasmaPCTG = (cmbTransactionType.SelectedValue.ToString().StartsWith("COA")) ? "Drybean extraction" : "Rendement";
                        _usrCtrOtherInfo = new usrCtrlOtherInfoSPU(this.WB_IN_DataSet, this.COMPCODE, this.WBNUM, this.YEAR, cmbSupplierCode, txbLocationID.Text, dtPickerTransactionDate.Value.Date, lblPotonganPlasmaPCTG);
                        break;
                    default:
                        if (dtRowTransType.TRANSTYPE.ToLower().Contains("cab"))
                        {
                            _usrCtrOtherInfo = new usrCtrlOtherInfoCPOIN(this.WB_IN_DataSet, this.COMPCODE, this.WBNUM, this.YEAR);
                        }
                        break;
                }

                if (OtherInfo != null)
                {
                    this.OtherInfo.SetConnection(this.SQLConnection);
                    this.OtherInfo.TransactionType = dtRowTransType.TRANSTYPE;
                    panelOtherInfo.Controls.Add(OtherInfo);
                    OtherInfo.Dock = DockStyle.Fill;
                }
            }
        }

        private void frmIN_FormClosing(object sender, FormClosingEventArgs e)
        {


            this.EndEdit();
            if (_isOverQuota || _isOverWeight)
                return;

            if (__isCloseCurrent)
            {
                this.ReleaseLock(this.WBNUM);
            }
            else
            {
                if (this.TransactionStatus != EnumTransactionStatus.POSTED && (ChangesMade && this.TransactionStatus != EnumTransactionStatus.CANCELED))
                {
                    String strMessage = "Do you want to save changes?";
                    if (this.IsNewData)
                        strMessage = "Do you want to save the new data?";

                    DialogResult dlgResult = MessageBox.Show(strMessage, this.Text, MessageBoxButtons.YesNoCancel);
                    if (dlgResult == DialogResult.Yes)
                    {

                        #region Validasi vehicle by jerry 07/04/2022 
                        // Perubahan pada validasi agar tidak ada validasi vehicle pada saat timbang out

                        bool isVehicleRegistered = true;

                        if (this.DataTimbang.IsIN)
                        {

                            var isSupplierVehicleActive = Data.clsMasterData.DL_WB_VENDORDataTable.Where(x => x.SUPPLIER == cmbSupplierCode.SelectedValue).FirstOrDefault();


                            if (isSupplierVehicleActive.ACTIVE == true && isSupplierVehicleActive.VEHICLE == true)
                            {
                                var vehicle = new Data.WB_MASTER_DataSetTableAdapters.DL_WB_VEHICLETableAdapter();
                                vehicle.SetConnection(Setting.SqlConnection);

                                var checkIsVehicleRegisteredTEST = vehicle.GetData().ToList();
                                var checkIsVehicleRegistered = vehicle.GetData().Where(x => x.VEHICLE_ID.Trim() == txbVehicleNo.Text.Trim()).FirstOrDefault();
                                if (checkIsVehicleRegistered == null)
                                {
                                    isVehicleRegistered = false;
                                    MessageBox.Show("Vehicle No. is  not registered. Please Contact Your Administrator!"
                                        , "Vehicle Lisence Number", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    btnSave.Enabled = false;
                                }
                                else if (checkIsVehicleRegistered.ESTATE != PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode
                                    )
                                {
                                    isVehicleRegistered = false;
                                    MessageBox.Show("Vehicle No. is not for Mill " + PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode
                                        + ". Please Contact Your Administrator!"
                                        , "Vehicle Lisence Number", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    btnSave.Enabled = false;
                                }
                            }
                        }
                        #endregion

                        if (this.HasError || isVehicleRegistered == false)
                        {
                            DisplayErrors();
                            e.Cancel = true;
                        }
                        else
                        {
                            //Add validation weight pada tombol save request by fandy - Agvin 310119                            
                            bool isDuplicateVehicle = false;
                            bool isValidateWeightOk = true;

                            if (this.IsNewData)
                            {
                                string dtTransDate = dtPickerTransactionDate.Value.ToString("yyyy/MM/dd");
                                string dtNetIn = dateTimePicker1.Value.ToString("yyyy/MM/dd");

                                //Fernando, 15/12/2016 -> Validasi Over Weight
                                var WBInWeight = new Data.WB_MASTER_DataSetTableAdapters.DL_WB_IN_WEIGHTTableAdapter();
                                WBInWeight.SetConnection(PR_WEIGHTBRIDGE.Application.Business.clsHelper.GlobalConnection);

                                var queryWBInWeight = WBInWeight.GetDataByEstateTranstypeDate(PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode,
                                                                cmbTransactionType.SelectedValue.ToString(), Convert.ToDateTime(dtTransDate));

                                if (queryWBInWeight.Rows.Count > 0)
                                {
                                    decimal maxweight = queryWBInWeight.Rows[0].ItemArray[5] != null ? Convert.ToDecimal(queryWBInWeight.Rows[0].ItemArray[5]) : 0;
                                    if (maxweight > 0 && (Convert.ToDecimal(txbWeightIn.Text) >= maxweight))
                                    {
                                        _isOverWeight = true;
                                        MessageBox.Show("Cannot save because your Weight In is over weight for transaction type [" + cmbTransactionType.Text + "]\nMaximum Weight In is [" + (Convert.ToInt64(maxweight)).ToString() + "] kg", "Error Over Weight", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        btnSave.Enabled = false;
                                        e.Cancel = true;
                                        isValidateWeightOk = false;
                                    }
                                }
                                //end sedang cek validasi vendor quota

                                //Fernando, 21/05/2013, Cek AllowVendorQuota

                                bool AllowVendorQuota = PR_WEIGHTBRIDGE.Application.Business.clsLoginInfo.Privilege.AllowEditVendorQuota;
                                if (AllowVendorQuota && !_isOverWeight)
                                {
                                    var AccountingPeriodTA = new Data.WB_MASTER_DataSetTableAdapters.GET_ACCOUNTING_PERIODTableAdapter();
                                    var VendorQuotaTA = new Data.WB_MASTER_DataSetTableAdapters.DL_WB_VENDOR_QUOTATableAdapter();
                                    var QueriesTA = new Data.WB_MASTER_DataSetTableAdapters.QueriesTableAdapter();
                                    var WBInPendingTA = new Data.WB_MASTER_DataSetTableAdapters.DL_WB_IN_TIMBANGTableAdapter();
                                    var VendorTA = new Data.WB_MASTER_DataSetTableAdapters.DL_WB_VENDORTableAdapter();

                                    AccountingPeriodTA.SetConnection(PR_WEIGHTBRIDGE.Application.Business.clsHelper.GlobalConnection);
                                    VendorQuotaTA.SetConnection(PR_WEIGHTBRIDGE.Application.Business.clsHelper.GlobalConnection);
                                    QueriesTA.SetConnection(PR_WEIGHTBRIDGE.Application.Business.clsHelper.GlobalConnection);
                                    WBInPendingTA.SetConnection(PR_WEIGHTBRIDGE.Application.Business.clsHelper.GlobalConnection);
                                    VendorTA.SetConnection(PR_WEIGHTBRIDGE.Application.Business.clsHelper.GlobalConnection);

                                    //Fernando, Check Vendor Quota by Transaction Type - DL_WB_VENDOR
                                    var VendorInfo = VendorTA.GetDataByCompEstateSupplier(PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPCompanyCode,
                                                        PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode,
                                                        cmbSupplier.SelectedValue.ToString()).FirstOrDefault();

                                    if (VendorInfo != null && VendorInfo.VENDOR_QUOTA)
                                    {
                                        if (Convert.ToDateTime(dtTransDate) > Convert.ToDateTime(dtNetIn))
                                            throw new ApplicationException("Transaction Date [" + dtTransDate + "]  shouldn't greater than " + " Date In [" + dtNetIn + "]");

                                        var PeriodInfo = AccountingPeriodTA.GetData(Convert.ToDateTime(dtNetIn));
                                        if (PeriodInfo.Count < 1) throw new ApplicationException("Period for " + dtPickerTransactionDate.Value.ToShortDateString() + " not found.");

                                        //Fernando, 7 Februari 2013 --> Cek Data Period Not Null
                                        if (PeriodInfo.Count > 0 && String.IsNullOrEmpty(PeriodInfo.Rows[0].ItemArray[1].ToString())) throw new ApplicationException("Period for " + dtPickerTransactionDate.Value.ToShortDateString() + " not found.");

                                        if (cmbSupplier.SelectedValue != null)
                                        {
                                            var VendorQuotaInfo = VendorQuotaTA.GetData((int)PeriodInfo.Rows[0].ItemArray[0], (int)PeriodInfo.Rows[0].ItemArray[1],
                                                PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode,
                                                cmbSupplier.SelectedValue.ToString());

                                            if (VendorQuotaInfo.Rows.Count > 0)
                                            {
                                                string dtTimeStartStr = Convert.ToDateTime(PeriodInfo.Rows[0].ItemArray[2]).ToString("yyyy/MM/dd") + " 00:00:01";
                                                string dtTimeEndStr = Convert.ToDateTime(PeriodInfo.Rows[0].ItemArray[3]).ToString("yyyy/MM/dd") + " 23:59:59";

                                                string dtTimeStartFinal = Convert.ToDateTime(dtTimeStartStr).ToString("yyyy/MM/dd hh:mm:ss tt");
                                                string dtTimeEndFinal = Convert.ToDateTime(dtTimeEndStr).ToString("yyyy/MM/dd hh:mm:ss tt");


                                                var currentNet = QueriesTA.GetTotalNetBySupplierByPeriod(cmbSupplier.SelectedValue.ToString(), Convert.ToDateTime(dtTimeStartFinal), Convert.ToDateTime(dtTimeEndFinal));
                                                decimal quotaVendor = Convert.ToDecimal(VendorQuotaInfo.Rows[0].ItemArray[5]);
                                                var weightEstimate = QueriesTA.GetVendorQuotaWeightEstimateByEstate(PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode.ToString());
                                                var wbInStillPending = WBInPendingTA.GetWBInPending(cmbSupplier.SelectedValue.ToString(), Convert.ToDateTime(dtTimeStartFinal), Convert.ToDateTime(dtTimeEndFinal)).ToList();

                                                //Potong 500 jika WBIN < Estimasi FFB
                                                decimal weightEstimateLessEstimate = Convert.ToInt64(weightEstimate);

                                                if (Convert.ToDecimal(txbWeightIn.Text) < Convert.ToInt64(weightEstimate))
                                                {
                                                    weightEstimateLessEstimate = Convert.ToDecimal(txbWeightIn.Text) - 500;
                                                }

                                                //GET WbIN Still Pending
                                                decimal currWeightStillPending = 0;
                                                foreach (var dataPending in wbInStillPending)
                                                {
                                                    if (Convert.ToInt64(dataPending.TIMBANG1) < Convert.ToInt64(weightEstimate))
                                                    {
                                                        currWeightStillPending += Convert.ToInt64(dataPending.TIMBANG1) - 500;
                                                    }
                                                    else
                                                    {
                                                        currWeightStillPending += Convert.ToInt64(dataPending.TIMBANG1) - Convert.ToInt64(weightEstimate);
                                                    }
                                                }

                                                decimal currWeight = 0;
                                                if (currWeightStillPending > 0)
                                                {
                                                    if (Convert.ToDecimal(txbWeightIn.Text) < Convert.ToInt64(weightEstimate))
                                                    {
                                                        currWeight = quotaVendor - Convert.ToInt64(currentNet) - Convert.ToInt64(weightEstimateLessEstimate) - Convert.ToInt64(currWeightStillPending);
                                                    }
                                                    else
                                                    {
                                                        currWeight = quotaVendor - Convert.ToInt64(currentNet) - (Convert.ToDecimal(txbWeightIn.Text) - Convert.ToInt64(weightEstimate)) - Convert.ToInt64(currWeightStillPending);
                                                    }
                                                }
                                                else
                                                {
                                                    if (Convert.ToDecimal(txbWeightIn.Text) < Convert.ToInt64(weightEstimate))
                                                    {
                                                        currWeight = quotaVendor - Convert.ToInt64(currentNet) - Convert.ToInt64(weightEstimateLessEstimate);
                                                    }
                                                    else
                                                    {
                                                        currWeight = quotaVendor - Convert.ToInt64(currentNet) - (Convert.ToDecimal(txbWeightIn.Text) - Convert.ToInt64(weightEstimate));
                                                    }
                                                }


                                                string errorMessage = string.Empty;
                                                //decimal currWeight = quotaVendor - Convert.ToInt64(currentNet) - Convert.ToInt64(weightEstimate) - Convert.ToInt64(currentWbInNotHaveNet);
                                                if (currWeight < 0 && Convert.ToInt64(currWeightStillPending) > 0)
                                                {
                                                    //decimal currWeightCurrent = quotaVendor - Convert.ToInt64(currentNet) - (Convert.ToInt64(totalcurrentWbInNotHaveNet) - (Convert.ToInt64(totalRowcurrentWbInNotHaveNet) * Convert.ToInt64(weightEstimate)));
                                                    decimal currWeightCurrent = quotaVendor - Convert.ToInt64(currentNet) - Convert.ToInt64(currWeightStillPending);
                                                    if (currWeightCurrent < 0 && Convert.ToInt64(currWeightStillPending) > 0)
                                                    {
                                                        //MessageBox.Show("Cannot save because supplier " + cmbSupplier.SelectedValue.ToString() + " is over quota for current period. Current total net is " + currentNet.ToString() + ". Please finish some transaction still pending that don't have net weight, the total weight still pending is " + totalcurrentWbInNotHaveNet.ToString(), "Error Supplier Quota", MessageBoxButtons.OK, MessageBoxIcon.Error);

                                                        //decimal totalGrossStillPending = (Convert.ToInt64(totalcurrentWbInNotHaveNet) - (Convert.ToInt64(totalRowcurrentWbInNotHaveNet) * Convert.ToInt64(weightEstimate)));

                                                        errorMessage += "Cannot save because supplier " + cmbSupplier.SelectedValue.ToString() + " is over quota for current period. \n";
                                                        errorMessage += "Current Total Net : " + (Convert.ToInt64(currentNet)).ToString() + "\n";
                                                        errorMessage += "Total weight still pending is : " + (Convert.ToInt64(currWeightStillPending)).ToString() + "\n";

                                                        //MessageBox.Show("Cannot save because supplier " + cmbSupplier.SelectedValue.ToString() + " is over quota for current period. /n");
                                                        MessageBox.Show(errorMessage, "Error Supplier Quota", MessageBoxButtons.OK, MessageBoxIcon.Error);

                                                        btnSave.Enabled = false;
                                                        e.Cancel = true;
                                                        isValidateWeightOk = false;
                                                        _isOverQuota = true;
                                                    }
                                                }
                                                else if (currWeight < 0)
                                                {
                                                    decimal currWeightNettCurrent = quotaVendor - Convert.ToInt64(currentNet);
                                                    if (Convert.ToInt64(currWeightNettCurrent) < 0)
                                                    {
                                                        MessageBox.Show("Cannot save because supplier " + cmbSupplier.SelectedValue.ToString() + " is over quota for current period.  Current total net for this period is " + (Convert.ToInt64(currentNet)).ToString(), "Error Supplier Quota", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                        btnSave.Enabled = false;
                                                        e.Cancel = true;
                                                        isValidateWeightOk = false;
                                                        _isOverQuota = true;
                                                    }
                                                }

                                            }
                                            else
                                            {
                                                MessageBox.Show("Please set Vendor Quota for this period", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                btnSave.Enabled = false;
                                                e.Cancel = true;
                                                _isOverQuota = true;
                                            }
                                        }
                                    }
                                }

                                //end sedang cek validasi vendor quota
                                //End validation weight pada tombol save request by fandy - Agvin 310119
                            }
                            else
                            {
                                //if WB timbang OUT
                                if (_qrCodeDisplay) GenerateQrcode();
                            }

                            //Fernando, Validasi No Kendaraan
                            string strErrorVehicleMessage = string.Empty;
                            if (!String.IsNullOrEmpty(txbVehicleNo.Text))
                            {
                                var WBInTA = new Data.WB_IN_DataSetTableAdapters.DL_WB_INTableAdapter();
                                var WBINTimbangTA = new Data.WB_IN_DataSetTableAdapters.DL_WB_IN_TIMBANGTableAdapter();
                                var WBInRegTA = new Data.WB_IN_DataSetTableAdapters.DL_WB_IN_REGTableAdapter();
                                var WBOutTA = new Data.WB_IN_DataSetTableAdapters.DL_WB_IN_REGTableAdapter();
                                var WBVendorTA = new Data.WB_MASTER_DataSetTableAdapters.DL_WB_VENDORTableAdapter();

                                WBInTA.SetConnection(PR_WEIGHTBRIDGE.Application.Business.clsHelper.GlobalConnection);
                                WBINTimbangTA.SetConnection(PR_WEIGHTBRIDGE.Application.Business.clsHelper.GlobalConnection);
                                WBInRegTA.SetConnection(PR_WEIGHTBRIDGE.Application.Business.clsHelper.GlobalConnection);
                                WBOutTA.SetConnection(PR_WEIGHTBRIDGE.Application.Business.clsHelper.GlobalConnection);
                                WBVendorTA.SetConnection(PR_WEIGHTBRIDGE.Application.Business.clsHelper.GlobalConnection);



                                //Fernando, Check Supplier by TransactionType
                                var supplierData = WBVendorTA.GetDataByCompcodeEstateTrxtypeSupplier(PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPCompanyCode.ToString(),
                                                                                                        PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode.ToString(),
                                                                                                         cmbTransactionType.SelectedValue.ToString(), cmbSupplier.SelectedValue.ToString()).FirstOrDefault();

                                // Fungsi pengecualian transaction type terhadap supplier dengan kode dibawah ini added by jerry 24/02/2021
                                if (PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode.ToString() == "8187" ||
                                    PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode.ToString() == "8189" ||
                                    PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode.ToString() == "8190" ||
                                    PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode.ToString() == "8191" ||
                                    PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode.ToString() == "8197"
                                    )
                                {
                                    //do action
                                    supplierData = WBVendorTA.GetDataByCompEstateSupplier(PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPCompanyCode.ToString(),
                                                                                                        PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode.ToString(),
                                                                                                        cmbSupplier.SelectedValue.ToString()).FirstOrDefault();
                                }
                                var datetest1 = DateTime.Now;

                                if (supplierData != null)
                                {
                                    //string millCode = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode;
                                    var queryWBInReg = WBInRegTA.GetDataByCompCodeYear(this.COMPCODE, this.YEAR).ToList();
                                    var queryWBIn = WBInTA.GetDataByCompCodeYear(this.COMPCODE, this.YEAR).Where(x => x.STATUS == "").ToList(); //.GetData().Where(x => x.ESTATE == millCode && x.YEAR == this.YEAR && x.STATUS == "");

                                    var queryJOIN1 = (from a in queryWBInReg
                                                      join b in queryWBIn on a.WBNUM equals b.WBNUM
                                                      where a.NOPOL == txbVehicleNo.Text
                                                      select a).ToList();



                                    //function baru untuk validasi correction yang lama karena sangat lambat by jerry 20/09/2021
                                    #region

                                    var queryWBInTimbangtest = WBINTimbangTA.GetDataByCompCodeYear
                                    (this.COMPCODE, this.YEAR).Where(x => x.TIMBANG1 != 0 && x.TIMBANG2 == 0).ToList();
                                    List<String> listWBNUM1 = new List<String>();

                                    foreach (var a in queryJOIN1)
                                    {
                                        listWBNUM1.Add(a.WBNUM);
                                    }

                                    foreach (var b in queryWBInTimbangtest)
                                    {
                                        if (listWBNUM1.Contains(b.WBNUM))
                                        {
                                            if (b.WBNUM != this.WBNUM)
                                            {
                                                if (!String.IsNullOrEmpty(strErrorVehicleMessage))
                                                    strErrorVehicleMessage += "\n";

                                                strErrorVehicleMessage += "Vehicle No [" + txbVehicleNo.Text + "] - WBNUM [" + b.WBNUM + "] - Transaction Date [" + b.WBDATE1.ToString("dd-MM-yyyy") + "]";
                                            }
                                        }
                                    }
                                    #endregion

                                    //Function lama yang digantikan oleh function baru diatas
                                    #region
                                    //foreach (var a in queryJOIN1)
                                    //{
                                    //    var queryWBInTimbang = WBINTimbangTA.GetDataByCompCodeYear
                                    //        (this.COMPCODE, this.YEAR).Where(x => x.WBNUM == a.WBNUM && x.TIMBANG1 != null && x.TIMBANG2 == null).FirstOrDefault();
                                    //    if (queryWBInTimbang != null && !queryWBInTimbang.IsTIMBANG1Null() && queryWBInTimbang.IsTIMBANG2Null() &&
                                    //            (queryWBInTimbang.WBNUM != this.WBNUM))
                                    //    {
                                    //        if (!String.IsNullOrEmpty(strErrorVehicleMessage))
                                    //            strErrorVehicleMessage += "\n";

                                    //        strErrorVehicleMessage += "Vehicle No [" + txbVehicleNo.Text + "] - WBNUM [" + a.WBNUM + "] - Transaction Date [" + a.WBDATE.ToString("dd-MM-yyyy") + "]";
                                    //    }
                                    //}
                                    #endregion //


                                    if (!String.IsNullOrEmpty(strErrorVehicleMessage))
                                    {
                                        isDuplicateVehicle = true;
                                        MessageBox.Show("These Vehicle No already exist and don't have Weight Out data : \n" + strErrorVehicleMessage, "Error Vehicle No", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        e.Cancel = true;
                                    }

                                    if (!isDuplicateVehicle && isValidateWeightOk)
                                    {
                                        //JIKA PROSES BERHASIL
                                        this.WB_IN.Active = false;
                                        this.WB_OUT.Active = false;
                                        string ticket_number = string.Empty;

                                        //Check Flag
                                        if (this.label5.Visible)
                                        {
                                            this.Save(out ticket_number, 1);
                                        }
                                        else
                                        {
                                            this.Save(out ticket_number, 0);
                                        }

                                        //MENYIMPAN GAMBAR=======================================
                                        var noPolValueRead = textBox1.Text;
                                        var noPolImageRead = label6.Text;
                                        var noPolFullImageRead = label7.Text;

                                        string base64String = string.Format($"{noPolImageRead}");
                                        string base64FullString = string.Format($"{noPolFullImageRead}");
                                        // Konversi Base64 string menjadi array byte
                                        byte[] imageBytes = Convert.FromBase64String(base64String);
                                        byte[] fullImageBytes = Convert.FromBase64String(base64FullString);

                                        // Path folder root dengan nama "img"
                                        string rootPath = AppDomain.CurrentDomain.BaseDirectory;
                                        string imgFolderPath = Path.Combine(rootPath, "img");

                                        // Membuat folder jika belum ada
                                        if (!Directory.Exists(imgFolderPath))
                                        {
                                            Directory.CreateDirectory(imgFolderPath);
                                        }

                                        // Nama file untuk menyimpan gambar, bisa menggunakan waktu saat ini
                                        string fileName = $"{ticket_number}_IN.jpg";
                                        string filePath = Path.Combine(imgFolderPath, fileName);

                                        // Menyimpan file gambar ke folder "img"
                                        File.WriteAllBytes(filePath, imageBytes);

                                        // Nama file untuk menyimpan gambar full image, bisa menggunakan waktu saat ini agar unik
                                        string fullFileName = $"{ticket_number}_FULL_IN.jpg";
                                        string fullFilePath = Path.Combine(imgFolderPath, fullFileName);

                                        // Menyimpan file gambar full image ke folder "img"
                                        File.WriteAllBytes(fullFilePath, fullImageBytes);

                                        //this.WB_IN_DataSet.DL_WB_CAPTURE
                                        InsertDL_WB_CAPTURE(ticket_number, txbVehicleNo.Text, noPolValueRead, DateTime.Now);

                                        this.ReleaseLock(this.WBNUM);
                                        bool _print = false;

                                        if (!this.DataTimbang.IsIN && !this.DataTimbang.IsOUT)
                                        {
                                            if (this.DataHeader.IsPRINTNull() || this.DataHeader.PRINT == 0)
                                            {
                                                if (PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.PrintAfterWBOUT)
                                                {
                                                    Reload();
                                                    btnPrintTicket_Click(this.btnPrintTicket, EventArgs.Empty);
                                                    _print = true;
                                                }
                                            }
                                        }

                                        if (!this.DataTimbang.IsIN && this.DataTimbang.IsOUT && System.Text.RegularExpressions.Regex.IsMatch(DataRegistrasi.TRANSTYPE.ToLower(), "teb[1-3]"))
                                        {
                                            //YS 20110617
                                            if (PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.LabelPrintAfterWBOUT || PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.BrutoPrintAfterWBOUT)
                                            {
                                                PrintLabelANDBruto();
                                            }
                                        }

                                        if (!_print && OnSaveComplete != null)
                                            OnSaveComplete(this, EventArgs.Empty);

                                    }

                                }
                                else
                                {
                                    MessageBox.Show("These Supplier " + cmbSupplier.SelectedValue.ToString() + " doesn't match with Transaction Type " + cmbTransactionType.SelectedValue.ToString(), "Error Supplier Code", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    e.Cancel = true;
                                }
                            }

                        }

                    }
                    else if (dlgResult == DialogResult.Cancel)
                    {
                        e.Cancel = true;
                    }
                    else if (dlgResult == DialogResult.No)
                    {
                        this.ReleaseLock(this.WBNUM);
                    }
                }
                else
                {
                    this.ReleaseLock(this.WBNUM);
                }
            }

            if (!e.Cancel)
            {
                #region Turn off NFC function when form is closed 
                // untuk reset function nfc agar tidak tetap terbawa setelah menutup form ataupun terbawa saat membuka form baru
                //acr122u.CardInserted -= Acr122u_CardInserted;
                //acr122u.CardRemoved -= Acr122u_CardRemoved;
                //acr122u.ReadText = null;
                #endregion

                // {
                WB_IN.OnSuccessEvent -= WB_IN_OnSuccessEvent;
                WB_IN.OnFailedEvent -= WB_IN_OnFailedEvent;
                //}
                //else if (this.DataTimbang.IsOUT)
                //{
                WB_OUT.OnSuccessEvent -= WB_OUT_OnSuccessEvent;
                WB_OUT.OnFailedEvent -= WB_OUT_OnFailedEvent;
                //}

                WB_IN.CloseReader();
                WB_OUT.CloseReader();
            }
        }

        private void InsertDL_WB_CAPTURE(string ticketNo, string nopolSPBS, string nopolCCTV, DateTime createDate)
        {
            try
            {
                // Inisialisasi baris baru untuk DL_WB_CAPTURE
                WB_IN_DataSet.DL_WB_CAPTURERow newRow = this.WB_IN_DataSet.DL_WB_CAPTURE.NewDL_WB_CAPTURERow();
                newRow.TICKET_NO = ticketNo;
                newRow.NOPOL_SPBS = nopolSPBS;
                newRow.NOPOL_CCTV = nopolCCTV;
                newRow.CREATE_DATE = createDate;

                // Buka koneksi jika belum terbuka
                if (this.SQLConnection.State != ConnectionState.Open)
                    this.SQLConnection.Open();

                // Mulai transaksi
                using (System.Data.SqlClient.SqlTransaction sqlTransaction = this.SQLConnection.BeginTransaction())
                {
                    try
                    {
                        // Set transaksi untuk table adapter
                        this.DL_WB_CAPTURETableAdapter.SetTransaction(sqlTransaction);

                        // Tambahkan baris baru ke dataset
                        this.WB_IN_DataSet.DL_WB_CAPTURE.AddDL_WB_CAPTURERow(newRow);

                        // Simpan perubahan ke database
                        this.DL_WB_CAPTURETableAdapter.Update(this.WB_IN_DataSet.DL_WB_CAPTURE);

                        // Commit transaksi jika berhasil
                        sqlTransaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaksi jika terjadi kesalahan
                        sqlTransaction.Rollback();
                        Console.WriteLine("Error: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        private void txbRefuseKeyPress_EventHandler(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        bool _isPrintReady = false;
        private void btnPrintTicket_Click(object sender, EventArgs e)
        {
            PR_WEIGHTBRIDGE.Application.Business.Print.clsPrintHelper.Paper _paper = PR_WEIGHTBRIDGE.Application.Business.Print.clsPrintHelper.Paper.PLAIN;
            if (PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.UsePreprintedIN)
                _paper = PR_WEIGHTBRIDGE.Application.Business.Print.clsPrintHelper.Paper.PREPRINTED;

            int _int_count = this.DataHeader.IsPRINTNull() ? 0 : this.DataHeader.PRINT;
            string millCode = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode;

            Microsoft.Reporting.WinForms.LocalReport _ticket = new Microsoft.Reporting.WinForms.LocalReport();
            Business.Print.clsPrintHelper.LoadINTicket(_ticket
                , _paper
                , this.COMPCODE
                , millCode
                , this.WBNUM
                , this.YEAR
                , _int_count
                );
            Utility.clsReportPrintDocument _printDocument = new PR_WEIGHTBRIDGE.Utility.clsReportPrintDocument(_ticket);
            _printDocument.EndPrint += new System.Drawing.Printing.PrintEventHandler(_printDocument_EndPrint);
            _isPrintReady = false;
            if (PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.UsePrintPreviewIN)
            {
                PrintPreviewDialog _preview_dialog = new PrintPreviewDialog();
                _preview_dialog.Document = _printDocument;
                _preview_dialog.ShowDialog(this);
            }
            else
            {
                _isPrintReady = true;
                _printDocument.Print();
            }
        }
        void _printDocument_EndPrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            if (_isPrintReady)
            {
                this.WB_IN_DataSet.DL_WB_IN.IsPosting = true;
                this.DataHeader.PRINT = this.DataHeader.IsPRINTNull() ? 1 : this.DataHeader.PRINT + 1;
                this.DL_WB_INTableAdapter.Update(this.DataHeader);
                this.WB_IN_DataSet.DL_WB_IN.IsPosting = true;
                this.Reload();
                this.SetUI();

                if (OnSaveComplete != null)
                    OnSaveComplete(this, EventArgs.Empty);
            }
            else
                _isPrintReady = true;
        }

        private void btn_Post_Click(object sender, EventArgs e)
        {
            backgroundWorker.RunWorkerAsync();
            Splash.ShowDialog();
        }


        private void tabControlRegistration_SizeChanged(object sender, EventArgs e)
        {
            tabControlRegistration.SelectedTab.Refresh();
        }

        private void dgvDocument_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            //DataGridView dgv = sender as DataGridView;
            //DataRowView drv = dgv.CurrentRow.DataBoundItem as DataRowView;
            //PR_WEIGHTBRIDGE.Data.WB_IN_DataSet.DL_WB_IN_DOCRow dr = drv.Row as PR_WEIGHTBRIDGE.Data.WB_IN_DataSet.DL_WB_IN_DOCRow;
            //dr.SetColumnError("REFDOC", "Error");  
        }

        private void dgvDocument_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            //MessageBox.Show(this, e.Exception.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            //e.Cancel = true;
        }

        private void tabControlRegistration_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.DisplayErrors();
        }

        private void cmbDeviceIN_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DataTimbang != null)
            {
                if (DataTimbang.IsIN)
                {
                    ComboBox _cmb = sender as ComboBox;
                    try
                    {
                        WB_OUT.CloseReader();
                        DataTimbang.WBCODE1 = _cmb.SelectedValue.ToString();
                        WB_IN.SetReader((WBReader.ReadFromSerial)_cmb.SelectedItem);
                        WB_IN.ReadWeight();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException(ex);
                        MessageBox.Show(this, string.Format(ex.Message), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                }
            }
        }

        private void cmbDeviceOUT_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DataTimbang != null)
            {
                if (DataTimbang.IsOUT)
                {
                    ComboBox _cmb = sender as ComboBox;
                    try
                    {
                        WB_IN.CloseReader();
                        DataTimbang.WBCODE2 = _cmb.SelectedValue.ToString();
                        WB_OUT.SetReader((WBReader.ReadFromSerial)_cmb.SelectedItem);
                        WB_OUT.ReadWeight();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException(ex);
                        MessageBox.Show(this, string.Format(ex.Message), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                }
            }
        }

        private void cmbSupplier_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DL_WB_IN_REGbindingSource.Current != null)
            {
                //this.DL_WB_IN_DOCBindingSource.Clear();
                if (!this.IsNewData)
                    foreach (WB_IN_DataSet.DL_WB_IN_DOCRow row in this.WB_IN_DataSet.DL_WB_IN_DOC)
                        row.Delete();
                //if (string.IsNullOrEmpty(((DL_WB_IN_REGbindingSource.Current as DataRowView).Row as Data.WB_IN_DataSet.DL_WB_IN_REGRow).SUPPLIER))
                //{
                if (sender == cmbSupplier)
                {
                    if (cmbSupplier.SelectedValue != null)
                    {
                        cmbSupplier.DataBindings["SelectedValue"].WriteValue();
                        cmbSupplierCode.DataBindings["SelectedValue"].ReadValue();
                    }
                }
                else if (sender == cmbSupplierCode)
                {
                    if (cmbSupplierCode.SelectedValue != null)
                    {
                        cmbSupplierCode.DataBindings["SelectedValue"].WriteValue();
                        cmbSupplier.DataBindings["SelectedValue"].ReadValue();
                    }
                }
                //}
            }
            try
            {
                if (cmbTransactionType.SelectedValue != null && cmbSupplier.SelectedValue != null)
                {
                    DataRowView dtRowViewTransType = cmbTransactionType.SelectedItem as DataRowView;
                    PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSet.DL_WB_TRANSTYPE_INRow dtRowTransType = dtRowViewTransType.Row as PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSet.DL_WB_TRANSTYPE_INRow;

                    DataRowView dtRowViewSupplier = cmbSupplier.SelectedItem as DataRowView;
                    if (dtRowViewSupplier != null)
                    {
                        PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSet.DL_WB_VENDORRow dtRowSupplier = dtRowViewSupplier.Row as PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSet.DL_WB_VENDORRow;

                        this.panelPONum.Enabled = (dtRowTransType.PONUM_ON && dtRowSupplier.PONUM_ON) && this.TransactionStatus != EnumTransactionStatus.POSTED && this.TransactionStatus != EnumTransactionStatus.CANCELED;
                        this.panelPONum.Visible = (dtRowTransType.PONUM_ON && dtRowSupplier.PONUM_ON);
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {

            }
        }

        private void panelPONum_EnabledChanged(object sender, EventArgs e)
        {
            this.txbPONum.Clear();
        }

        private void btnCancelTransaction_Click(object sender, EventArgs e)
        {
            DialogResult _dlgResult = MessageBox.Show(this, "Do you want to cancel the transaction?", this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (_dlgResult == DialogResult.Yes)
            {
                try
                {
                    PR_WEIGHTBRIDGE.Data.WB_IN_DataSet.DL_WB_IN_POST_LOGRow row = this.WB_IN_DataSet.DL_WB_IN_POST_LOG.NewDL_WB_IN_POST_LOGRow();
                    row.TYPE = "C";
                    row.COMPCODE = this.DataRegistrasi.COMPCODE;
                    row.WBNUM = this.DataRegistrasi.WBNUM;
                    row.YEAR = this.DataRegistrasi.YEAR;
                    row.ID = this.DataRegistrasi.WBNUM;
                    row.NUMBER = 0;
                    row.MESSAGE = "Canceled w/o posting";

                    if (this.SQLConnection.State != ConnectionState.Open)
                        this.SQLConnection.Open();
                    System.Data.SqlClient.SqlTransaction sqlTransaction = this.SQLConnection.BeginTransaction();

                    this.DL_WB_POST_LOGTableAdapter.SetTransaction(sqlTransaction);
                    this.DL_WB_INTableAdapter.SetTransaction(sqlTransaction);

                    this.WB_IN_DataSet.DL_WB_IN_POST_LOG.AddDL_WB_IN_POST_LOGRow(row);
                    this.DL_WB_POST_LOGTableAdapter.Update(this.WB_IN_DataSet.DL_WB_IN_POST_LOG);

                    this.DataHeader.STATUS = "C";
                    System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                    Logger.BeginStopwatchedLog("Cancel IN", watch);

                    this.DL_WB_INTableAdapter.Update(this.DataHeader);
                    sqlTransaction.Commit();

                    Logger.EndStopwatchedLog("Cancel IN", watch);

                    this.TransactionStatus = EnumTransactionStatus.CANCELED;
                    this.SetUI();
                    if (OnSaveComplete != null)
                        OnSaveComplete(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                    throw ex;
                }
                finally
                {
                    if (this.OnTransactionCanceled != null)
                        OnTransactionCanceled(this, EventArgs.Empty);
                }
            }
        }

        private void btnCorrection_Click(object sender, EventArgs e)
        {
            this.panelTransactionDate.Enabled = true;
            this.panelTransactionType.Enabled = true;
            this.panelSupplier.Enabled = true;
            this.panelReferenceDocument.Enabled = true;
            this.panelDgvDocument.Enabled = true;
            this.panelVehicleNo.Enabled = true;
            this.panelDriverName.Enabled = true;

            this.cmbTransactionType.Enabled = true;
            this.cmbSupplierCode.Enabled = true;
            this.cmbSupplier.Enabled = true;
            this.cmbReferenceDocument.Enabled = true;
            this.dgvDocument.Enabled = true;
            this.txbVehicleNo.Enabled = true;
            this.txbDriverName.Enabled = true;


            this.DataHeader.STATUS = "R";

            //this.panelStorageLocation.Enabled = true;

            this.panelEstate.Enabled = true;
            this.panelPONum.Enabled = true;
            this.btnCancelTransaction.Enabled = false;
            this.btnPrintTicket.Enabled = false;
            this.btnCorrection.Enabled = false;

            //YS 20111213
            this.txbWeightIn.Enabled = true;
            this.txbWeightOut.Enabled = true;

            //YS 20120131
            this.panelStorageLocation.Enabled = true;
            this.cmbStorageLocation.Enabled = true;

            if (this.OtherInfo != null)
                this.OtherInfo.Enabled = true;

            //YS 20120202 able to edit weight in and weight out
            this.groupBoxWeightIN.Enabled = PR_WEIGHTBRIDGE.Application.Business.clsLoginInfo.Privilege.AllowEditWeight;
            this.groupBoxWeightOUT.Enabled = PR_WEIGHTBRIDGE.Application.Business.clsLoginInfo.Privilege.AllowEditWeight;
            this.panelDetailNote.Enabled = true;

            for (int i = 0; i < dgvDocument.RowCount; i++)
            {
                dgvDocument.Rows[i].Cells[0].ReadOnly = false;
                dgvDocument.Rows[i].Cells[1].ReadOnly = false;
            }

        }

        private void SetAdded()
        {
            this.DataHeader.SetAdded();
            this.DataRegistrasi.SetAdded();
            this.DataTimbang.SetAdded();
            //this.DataDoc.SetAdded();
            if (this.OtherInfo != null)
                this.OtherInfo.SetAdded();
            foreach (Data.WB_IN_DataSet.DL_WB_IN_DOCRow dtRowDoc in this.WB_IN_DataSet.DL_WB_IN_DOC)
            {
                dtRowDoc.SetAdded();
            }
        }
        private void SetLock(String pWBNUM)
        {
            if (!String.IsNullOrEmpty(pWBNUM))
            {
                PR_WEIGHTBRIDGE.Data.WB_LOCK_DataSetTableAdapters.DL_WB_LOCKED_WBTableAdapter daLock = new PR_WEIGHTBRIDGE.Data.WB_LOCK_DataSetTableAdapters.DL_WB_LOCKED_WBTableAdapter(this.SQLConnection);
                PR_WEIGHTBRIDGE.Data.WB_LOCK_DataSet.DL_WB_LOCKED_WBDataTable dtLock = new WB_LOCK_DataSet.DL_WB_LOCKED_WBDataTable();
                PR_WEIGHTBRIDGE.Data.WB_LOCK_DataSet.DL_WB_LOCKED_WBRow drLock = dtLock.NewDL_WB_LOCKED_WBRow();
                drLock.WBNUM = this.WBNUM;
                drLock.LOCK_DATE = DateTime.Now;
                drLock.LOGIN_ID = PR_WEIGHTBRIDGE.Application.Business.clsLoginInfo.LoginID;
                dtLock.AddDL_WB_LOCKED_WBRow(drLock);

                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                Logger.BeginStopwatchedLog(String.Format("Lock IN({0})", pWBNUM), watch);
                daLock.Update(dtLock);
                Logger.EndStopwatchedLog(String.Format("Lock IN({0})", pWBNUM), watch);
            }
            SetUI();
        }

        private void SetUI()
        {
            this.panelTransactionDate.Enabled = this.TransactionStatus != EnumTransactionStatus.ONPROGRESS && this.TransactionStatus != EnumTransactionStatus.POSTED && this.TransactionStatus != EnumTransactionStatus.CANCELED && this.TransactionStatus != EnumTransactionStatus.CORRECTION;
            this.panelTransactionType.Enabled = this.TransactionStatus != EnumTransactionStatus.ONPROGRESS && this.TransactionStatus != EnumTransactionStatus.POSTED && this.TransactionStatus != EnumTransactionStatus.CANCELED && this.TransactionStatus != EnumTransactionStatus.CORRECTION;
            //this.panelSupplier.Enabled = System.Text.RegularExpressions.Regex.IsMatch(this.DataRegistrasi.TRANSTYPE, "") || this.TransactionStatus != EnumTransactionStatus.POSTED && this.TransactionStatus != EnumTransactionStatus.CANCELED;
            this.panelSupplier.Enabled = this.TransactionStatus != EnumTransactionStatus.ONPROGRESS && this.TransactionStatus != EnumTransactionStatus.POSTED && this.TransactionStatus != EnumTransactionStatus.CANCELED;
            this.panelEstate.Enabled = this.TransactionStatus != EnumTransactionStatus.ONPROGRESS && this.TransactionStatus != EnumTransactionStatus.POSTED && this.TransactionStatus != EnumTransactionStatus.CANCELED;

            bool _ponum = false;
            if (cmbTransactionType.SelectedValue != null && cmbSupplier.SelectedValue != null)
            {
                DataRowView dtRowViewTransType = cmbTransactionType.SelectedItem as DataRowView;
                PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSet.DL_WB_TRANSTYPE_INRow dtRowTransType = dtRowViewTransType.Row as PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSet.DL_WB_TRANSTYPE_INRow;

                DataRowView dtRowViewSupplier = cmbSupplier.SelectedItem as DataRowView;
                if (dtRowViewSupplier != null)
                {
                    PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSet.DL_WB_VENDORRow dtRowSupplier = dtRowViewSupplier.Row as PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSet.DL_WB_VENDORRow;
                    _ponum = (dtRowTransType.PONUM_ON && dtRowSupplier.PONUM_ON);
                }
            }
            this.panelPONum.Visible = _ponum;
            this.panelPONum.Enabled = _ponum && this.TransactionStatus != EnumTransactionStatus.POSTED && this.TransactionStatus != EnumTransactionStatus.CANCELED && _ponum;
            this.panelReferenceDocument.Enabled = this.TransactionStatus != EnumTransactionStatus.POSTED && this.TransactionStatus != EnumTransactionStatus.CANCELED;
            this.panelDgvDocument.Enabled = this.TransactionStatus != EnumTransactionStatus.POSTED && this.TransactionStatus != EnumTransactionStatus.CANCELED;
            this.panelDriverName.Enabled = this.TransactionStatus != EnumTransactionStatus.POSTED && this.TransactionStatus != EnumTransactionStatus.CANCELED;
            this.panelVehicleNo.Enabled = this.TransactionStatus != EnumTransactionStatus.POSTED && this.TransactionStatus != EnumTransactionStatus.CANCELED;
            this.panelSPTA.Enabled = this.TransactionStatus != EnumTransactionStatus.POSTED && this.TransactionStatus != EnumTransactionStatus.CANCELED;
            this.panelNoteTimbang.Enabled = this.TransactionStatus != EnumTransactionStatus.POSTED && this.TransactionStatus != EnumTransactionStatus.CANCELED && this.TransactionStatus != EnumTransactionStatus.CORRECTION;
            this.panelDetailNote.Enabled = this.TransactionStatus != EnumTransactionStatus.POSTED && this.TransactionStatus != EnumTransactionStatus.CANCELED && this.TransactionStatus != EnumTransactionStatus.CORRECTION;
            this.panelStorageLocation.Enabled = this.TransactionStatus != EnumTransactionStatus.POSTED && this.TransactionStatus != EnumTransactionStatus.CANCELED && this.TransactionStatus != EnumTransactionStatus.CORRECTION;
            if (this.OtherInfo != null)
                this.OtherInfo.Enabled = this.TransactionStatus != EnumTransactionStatus.POSTED && this.TransactionStatus != EnumTransactionStatus.CANCELED && this.TransactionStatus != EnumTransactionStatus.CORRECTION;

            this.groupBoxWeightIN.Enabled =
                (
                 this.TransactionStatus == EnumTransactionStatus.ONPROGRESS ||
                this.TransactionStatus == EnumTransactionStatus.CORRECTION_ERROR
                || this.TransactionStatus == EnumTransactionStatus.PENDING
                || this.TransactionStatus == EnumTransactionStatus.POST_ERROR
                || this.TransactionStatus == EnumTransactionStatus.NEW)
                && (this.DataTimbang.IsIN || PR_WEIGHTBRIDGE.Application.Business.clsLoginInfo.Privilege.AllowEditWeight);

            this.groupBoxWeightOUT.Enabled =
                (
                 this.TransactionStatus == EnumTransactionStatus.ONPROGRESS ||
                this.TransactionStatus == EnumTransactionStatus.CORRECTION_ERROR
                || this.TransactionStatus == EnumTransactionStatus.PENDING
                || this.TransactionStatus == EnumTransactionStatus.POST_ERROR
                || this.TransactionStatus == EnumTransactionStatus.NEW)
                && (this.DataTimbang.IsOUT || PR_WEIGHTBRIDGE.Application.Business.clsLoginInfo.Privilege.AllowEditWeight);

            this.btn_Post.Enabled =
               (this.TransactionStatus == EnumTransactionStatus.PENDING
               || this.TransactionStatus == EnumTransactionStatus.CORRECTION
               || this.TransactionStatus == EnumTransactionStatus.POST_ERROR
               || this.TransactionStatus == EnumTransactionStatus.CORRECTION_ERROR)
               && PR_WEIGHTBRIDGE.Application.Business.clsLoginInfo.Privilege.AllowINPost;

            this.btnCorrection.Enabled =
                (
                this.TransactionStatus == EnumTransactionStatus.POSTED
                && this.TransactionStatus != EnumTransactionStatus.CANCELED
                && this.TransactionStatus != EnumTransactionStatus.CORRECTION)
                && PR_WEIGHTBRIDGE.Application.Business.clsLoginInfo.Privilege.AllowINCorrection;

            this.btnCancelTransaction.Enabled =
                (this.TransactionStatus == EnumTransactionStatus.POST_ERROR
                || this.TransactionStatus != EnumTransactionStatus.ONPROGRESS
                || this.TransactionStatus != EnumTransactionStatus.PENDING)
                &&
                (this.TransactionStatus != EnumTransactionStatus.NEW
                && this.TransactionStatus != EnumTransactionStatus.CANCELED
                && this.TransactionStatus != EnumTransactionStatus.CORRECTION
                && this.TransactionStatus != EnumTransactionStatus.POSTED
                && PR_WEIGHTBRIDGE.Application.Business.clsLoginInfo.Privilege.AllowINCancel);

            this.btnPrintTicket.Enabled =
               (this.TransactionStatus == EnumTransactionStatus.POSTED
               || this.TransactionStatus == EnumTransactionStatus.POST_ERROR
               || this.TransactionStatus == EnumTransactionStatus.CORRECTION_ERROR
               || this.TransactionStatus == EnumTransactionStatus.PENDING)
               &&
               ((this.DataHeader.IsPRINTNull() || this.DataHeader.PRINT < 1) || PR_WEIGHTBRIDGE.Application.Business.clsLoginInfo.Privilege.AllowINReprint);

            this.txbWeightIn.Enabled = PR_WEIGHTBRIDGE.Application.Business.clsLoginInfo.Privilege.AllowEditWeight;
            this.txbWeightOut.Enabled = PR_WEIGHTBRIDGE.Application.Business.clsLoginInfo.Privilege.AllowEditWeight;


            if (this.DataTimbang.IsWBCODE1Null() || string.IsNullOrEmpty(this.DataTimbang.WBCODE1))
                cmbDeviceIN.SelectedValue = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.DefaultWBIN;

            if (PR_WEIGHTBRIDGE.Application.Business.clsLoginInfo.Privilege.AllowDifferentWBOut)
            {
                if (this.DataTimbang.IsWBCODE2Null() || string.IsNullOrEmpty(this.DataTimbang.WBCODE2))
                    cmbDeviceOUT.SelectedValue = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.DefaultWBOUT;
            }
            if (this.DataTimbang.IsOUT)
                if (!PR_WEIGHTBRIDGE.Application.Business.clsLoginInfo.Privilege.AllowDifferentWBOut)
                {
                    this.cmbDeviceOUT.Enabled = false;
                    this.cmbDeviceOUT.SelectedValue = this.cmbDeviceIN.SelectedValue;
                }

            if (cmbTransactionType.SelectedItem != null)
            {
                DataRowView dtRowViewTransType = cmbTransactionType.SelectedItem as DataRowView;
                PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSet.DL_WB_TRANSTYPE_INRow dtRowTransType = dtRowViewTransType.Row as PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSet.DL_WB_TRANSTYPE_INRow;

                panelSupplier.Enabled = false;
                dgvDocument.Enabled = false;
                string millCode = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPMillCode;
                int intRefType = dtRowTransType.REFTYPE;
                if (intRefType == 1)
                {
                    if (cmbTransactionType.SelectedValue.ToString().Equals("TEB1") && millCode.Equals("8551"))
                    {
                        panelSupplier.Enabled = this.TransactionStatus != EnumTransactionStatus.ONPROGRESS && this.TransactionStatus != EnumTransactionStatus.POSTED && this.TransactionStatus != EnumTransactionStatus.CANCELED;
                        dgvDocument.Enabled = true;
                    }
                }
                else
                {
                    panelSupplier.Enabled = this.TransactionStatus != EnumTransactionStatus.ONPROGRESS && this.TransactionStatus != EnumTransactionStatus.POSTED && this.TransactionStatus != EnumTransactionStatus.CANCELED;
                    dgvDocument.Enabled = true;
                }


                //panelSupplier.Enabled = intRefType != 1 && this.TransactionStatus != EnumTransactionStatus.POSTED && this.TransactionStatus != EnumTransactionStatus.CANCELED; ;
                //dgvDocument.Enabled = intRefType != 1;
            }

        }

        private void ReleaseLock(String pWBNUM)
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            Logger.BeginStopwatchedLog(String.Format("Unlock IN({0})", pWBNUM), watch);
            PR_WEIGHTBRIDGE.Data.WB_LOCK_DataSetTableAdapters.DL_WB_LOCKED_WBTableAdapter daLock = new PR_WEIGHTBRIDGE.Data.WB_LOCK_DataSetTableAdapters.DL_WB_LOCKED_WBTableAdapter(this.SQLConnection);
            daLock.ReleaseLock(pWBNUM);
            Logger.EndStopwatchedLog(String.Format("Unlock IN({0})", pWBNUM), watch);
        }

        private frmSplash _frmSplash = null;
        private frmSplash Splash
        {
            get
            {
                if (_frmSplash == null)
                    _frmSplash = new frmSplash();
                return _frmSplash;
            }
        }
        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                _deletedDOC.Clear();
                //this.WB_IN_DataSet.DL_WB_IN_POST_LOG.Clear();
                this.DataHeader.eventSAPWSSuccess += new EventHandler<clsSAPWSEvent>(DataHeader_eventSAPWSSuccess);
                this.DataHeader.eventSAPWSError += new EventHandler<clsSAPWSEvent>(DataHeader_eventSAPWSError);
                this.DataHeader.eventSAPWSNoResult += new EventHandler<clsSAPWSEvent>(DataHeader_eventSAPWSNoResult);


                if (this.TransactionStatus == EnumTransactionStatus.PENDING || this.TransactionStatus == EnumTransactionStatus.POST_ERROR)
                {
                    ////Check double Posting WB Number
                    //Data.WB_MASTER_DataSetTableAdapters.DL_WB_FLAG_POSTTableAdapter flagAdapter = new PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSetTableAdapters.DL_WB_FLAG_POSTTableAdapter();
                    //flagAdapter.SetConnection(Setting.SqlConnection);

                    //Data.WB_MASTER_DataSet.DL_WB_FLAG_POSTDataTable flagDataTable = flagAdapter.GetData();
                    //if (flagDataTable.FindByCOMPCODEESTATEWBNUMYEAR(Setting.Compcode, Setting.Mill, txbTicketNo.Text, dtPickerTransactionDate.Value.Year.ToString()) != null)
                    //{
                    //    MessageBox.Show("WB number [" + txbTicketNo.Text + "] is on progress post to SAP", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    //}
                    //else
                    //{
                    //    Data.WB_MASTER_DataSet.DL_WB_FLAG_POSTRow r = flagDataTable.NewDL_WB_FLAG_POSTRow();
                    //    r.COMPCODE = Setting.Compcode;
                    //    r.ESTATE = Setting.Mill;
                    //    r.WBNUM = txbTicketNo.Text;
                    //    r.POST_DATE = DateTime.Now;
                    //    r.YEAR = dtPickerTransactionDate.Value.Year.ToString();
                    //    r.CREATEDBY = Setting.UserId;
                    //    r.CREATEDDATE = DateTime.Now;
                    //    r.LASTUPDATEDBY = Setting.UserId;
                    //    r.LASTUPDATEDDATE = DateTime.Now;
                    //    flagDataTable.AddDL_WB_FLAG_POSTRow(r);
                    //    flagAdapter.Update(r);

                    //    throw new Exception("");

                    System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                    Logger.BeginStopwatchedLog("Posting IN", watch);
                    //this.DataHeader.Post(PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.WSDLUID, PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.WSDLPassword);
                    string pSAPClient = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPClient;
                    string pSAPPort = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPPort;
                    //if (pSAPClient == "800" && pSAPPort == "81")
                    //{
                    //    this.DataHeader.Post81(string.Empty, string.Empty);
                    //}
                    //else if (pSAPClient == "800" && pSAPPort == "82")
                    //{
                    //    this.DataHeader.Post82(string.Empty, string.Empty);
                    //}
                    //else
                    //{
                    this.DataHeader.Post(string.Empty, string.Empty, pSAPClient);
                    //}
                    Logger.EndStopwatchedLog("Posting IN", watch);


                    ////Delete Flag Post
                    //r.Delete();
                    //flagAdapter.Update(r);
                    //}
                }

                else if (this.TransactionStatus == EnumTransactionStatus.CORRECTION || this.TransactionStatus == EnumTransactionStatus.CORRECTION_ERROR)
                {
                    System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                    Logger.BeginStopwatchedLog("Correction IN", watch);
                    //this.DataHeader.Correction(PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.WSDLUID, PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.WSDLPassword);

                    string pSAPClient = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPClient;
                    string pSAPPort = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.SAPPort;
                    //if (pSAPClient == "800" && pSAPPort == "81")
                    //{
                    //    this.DataHeader.Correction81(string.Empty, string.Empty);
                    //}
                    //else if (pSAPClient == "800" && pSAPPort == "82")
                    //{
                    //    this.DataHeader.Correction82(string.Empty, string.Empty);
                    //}
                    //else
                    //{
                    this.DataHeader.Correction(string.Empty, string.Empty, pSAPClient);
                    //}


                    Logger.EndStopwatchedLog("Correction IN", watch);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                throw ex;
            }
            finally
            {
                //this.DataHeader.eventSAPWSSuccess += new EventHandler<clsSAPWSEvent>(DataHeader_eventSAPWSSuccess);
                //this.DataHeader.eventSAPWSError += new EventHandler<clsSAPWSEvent>(DataHeader_eventSAPWSError);
                //this.DataHeader.eventSAPWSNoResult += new EventHandler<clsSAPWSEvent>(DataHeader_eventSAPWSNoResult);
            }
        }

        void DataHeader_eventSAPWSNoResult(object sender, clsSAPWSEvent e)
        {
            backgroundWorker.ReportProgress(0, new Exception("Transaction returns no result."));
        }

        void DataHeader_eventSAPWSError(object sender, clsSAPWSEvent e)
        {
            Data.WB_IN_DataSet.DL_WB_IN_POST_LOGDataTable dtPostLog = new WB_IN_DataSet.DL_WB_IN_POST_LOGDataTable();
            System.Text.StringBuilder strBuilderMessage = new StringBuilder();
            this.WB_IN_DataSet.DL_WB_IN.IsPosting = true;
            foreach (PR_WEIGHTBRIDGE.Data.Posting.clsBapiret2 retBapiret2 in e.Return)
            {
                Data.WB_IN_DataSet.DL_WB_IN_POST_LOGRow drPostLog = dtPostLog.NewDL_WB_IN_POST_LOGRow();
                drPostLog.COMPCODE = this.COMPCODE;
                drPostLog.WBNUM = retBapiret2.Wbnum;
                drPostLog.YEAR = retBapiret2.Zyear;
                drPostLog.NUMBER = String.IsNullOrEmpty(retBapiret2.Number) ? 0 : Int32.Parse(retBapiret2.Number);
                drPostLog.SAPDOC = retBapiret2.Refdoc;

                drPostLog.LOGMSGNO = retBapiret2.LogMsgNo;
                drPostLog.LOGNO = retBapiret2.LogNo;

                drPostLog.PARAMETER = retBapiret2.Parameter;
                drPostLog.ID = retBapiret2.Id;
                drPostLog.FIELD = retBapiret2.Field;


                drPostLog.MESSAGE = retBapiret2.Message.Length > 100 ? retBapiret2.Message.Substring(0, 100) : retBapiret2.Message;
                strBuilderMessage.AppendLine(String.Format("[{0}] - {1}", drPostLog.WBNUM, retBapiret2.Message));
                drPostLog.MESSAGEV1 = retBapiret2.MessageV1;
                drPostLog.MESSAGEV2 = retBapiret2.MessageV2;
                drPostLog.MESSAGEV3 = retBapiret2.MessageV3;
                drPostLog.MESSAGEV4 = retBapiret2.MessageV4;

                drPostLog.TYPE = retBapiret2.Type;

                dtPostLog.AddDL_WB_IN_POST_LOGRow(drPostLog);
            }
            this.DataHeader.STATUS = e.Return.First().Type;

            if (this.SQLConnection.State != ConnectionState.Open)
                this.SQLConnection.Open();
            System.Data.SqlClient.SqlTransaction sqlTransaction = this.SQLConnection.BeginTransaction();
            try
            {
                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                Logger.BeginStopwatchedLog(String.Format("{0} ERROR", e.Action.ToString()), watch);
                this.DL_WB_POST_LOGTableAdapter.SetTransaction(sqlTransaction);
                this.DL_WB_POST_LOGTableAdapter.Update(dtPostLog);
                this.DL_WB_INTableAdapter.SetTransaction(sqlTransaction);
                this.DL_WB_INTableAdapter.Update(this.DataHeader);
                sqlTransaction.Commit();
                Logger.BeginStopwatchedLog(String.Format("{0} ERROR", e.Action.ToString()), watch);
                backgroundWorker.ReportProgress(0, new Exception(String.Format("Transaction failed:\n{0}", strBuilderMessage.ToString())));

            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                sqlTransaction.Rollback();
                backgroundWorker.ReportProgress(0, new Exception(String.Format("An error has occured while saving log to database. {0}", ex.Message)));
            }
            finally
            {
                sqlTransaction.Dispose();
                if (this.SQLConnection.State == ConnectionState.Open)
                    this.SQLConnection.Close();
                this.WB_IN_DataSet.DL_WB_IN.IsPosting = false;
            }
        }

        void DataHeader_eventSAPWSSuccess(object sender, clsSAPWSEvent e)
        {
            Data.WB_IN_DataSet.DL_WB_IN_POST_LOGDataTable dtPostLog = new WB_IN_DataSet.DL_WB_IN_POST_LOGDataTable();
            this.WB_IN_DataSet.DL_WB_IN.IsPosting = true;
            foreach (PR_WEIGHTBRIDGE.Data.Posting.clsBapiret2 retBapiret2 in e.Return)
            {
                Data.WB_IN_DataSet.DL_WB_IN_POST_LOGRow drPostLog = dtPostLog.NewDL_WB_IN_POST_LOGRow();
                drPostLog.COMPCODE = this.COMPCODE;
                drPostLog.WBNUM = retBapiret2.Wbnum;
                drPostLog.YEAR = retBapiret2.Zyear;
                drPostLog.NUMBER = Int32.Parse(retBapiret2.Number);
                drPostLog.SAPDOC = retBapiret2.Refdoc;
                this.DataHeader.SAPNUM = retBapiret2.Refdoc;
                drPostLog.LOGMSGNO = retBapiret2.LogMsgNo;
                drPostLog.LOGNO = retBapiret2.LogNo;

                drPostLog.PARAMETER = retBapiret2.Parameter;
                drPostLog.ID = retBapiret2.Id;
                drPostLog.FIELD = retBapiret2.Field;

                drPostLog.MESSAGE = retBapiret2.Message;
                drPostLog.MESSAGEV1 = retBapiret2.MessageV1;
                drPostLog.MESSAGEV2 = retBapiret2.MessageV2;
                drPostLog.MESSAGEV3 = retBapiret2.MessageV3;
                drPostLog.MESSAGEV4 = retBapiret2.MessageV4;

                drPostLog.TYPE = retBapiret2.Type;

                dtPostLog.AddDL_WB_IN_POST_LOGRow(drPostLog);
            }

            //this.DataHeader.STATUS = e.Return.First().Type;
            //Validasi Posting Tiket
            if (!String.IsNullOrEmpty(e.Return.First().Refdoc))
            {
                bool isError = false;
                if (e.Return.First().Refdoc.Trim().Equals("0000"))
                {
                    isError = true;
                }
                else
                {
                    if (!String.IsNullOrEmpty(e.Return.First().Refdoc) && e.Return.First().Refdoc.Length == 14)
                    {
                        string lineDOC = e.Return.First().Refdoc.Substring(10, 4);
                        try
                        {
                            if (!String.IsNullOrEmpty(lineDOC) && lineDOC.Length == 4)
                            {
                                if (Convert.ToDouble(lineDOC) > 0)
                                    isError = false;
                                else
                                    isError = true;
                            }
                            else
                            {
                                isError = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            isError = true;
                        }
                    }
                    else
                    {
                        isError = true;
                    }
                }

                if (isError)
                    this.DataHeader.STATUS = "E";
                else
                    this.DataHeader.STATUS = e.Return.First().Type;
            }
            else
            {
                this.DataHeader.STATUS = "E";
            }

            if (this.SQLConnection.State != ConnectionState.Open)
                this.SQLConnection.Open();

            System.Data.SqlClient.SqlTransaction sqlTransaction = this.SQLConnection.BeginTransaction();
            try
            {
                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                Logger.BeginStopwatchedLog(String.Format("{0} SUCCESS", e.Action.ToString()), watch);
                this.DL_WB_POST_LOGTableAdapter.SetTransaction(sqlTransaction);
                this.DL_WB_POST_LOGTableAdapter.Update(dtPostLog);
                this.DL_WB_INTableAdapter.SetTransaction(sqlTransaction);
                this.DL_WB_INTableAdapter.Update(this.DataHeader);
                sqlTransaction.Commit();
                Logger.EndStopwatchedLog(String.Format("{0} SUCCESS", e.Action.ToString()), watch);
                backgroundWorker.ReportProgress(0, "Transaction completed successfuly.");
                //NEED DELEGATE
                //this.btn_Post.Enabled = false;
            }
            catch (Exception ex)
            {
                sqlTransaction.Rollback();
                backgroundWorker.ReportProgress(0, new Exception(String.Format("An error has occured while saving log to database. {0}", ex.Message)));
            }
            finally
            {
                sqlTransaction.Dispose();
                if (this.SQLConnection.State == ConnectionState.Open)
                    this.SQLConnection.Close();
                this.WB_IN_DataSet.DL_WB_IN.IsPosting = false;
            }
        }

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState is Exception)
            {
                Exception ex = e.UserState as Exception;
                Logger.LogException(ex);
                MessageBox.Show(this, ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                backgroundWorker.CancelAsync();
            }
            else if (e.UserState is string)
            {
                String message = e.UserState as string;
                MessageBox.Show(this, message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Reload();
            if (OnPostComplete != null)
                OnPostComplete(this, EventArgs.Empty);
            Splash.Close();
        }

        private void txbTimbang_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox txb = sender as TextBox;
            if (e.KeyChar == 13)
            {
                txb.DataBindings["Text"].WriteValue();
                decimal timbang1 = DataTimbang.IsTIMBANG1Null() ? 0 : DataTimbang.TIMBANG1;
                decimal timbang2 = DataTimbang.IsTIMBANG2Null() ? 0 : DataTimbang.TIMBANG2;
                DataTimbang.NET = Math.Abs(timbang1 - timbang2);
                txbNet.DataBindings["Text"].ReadValue();
                DL_WB_IN_TIMBANGBindingSource.EndEdit();
            }
            if ((e.KeyChar < 48 || e.KeyChar > 57) && e.KeyChar != 8 && e.KeyChar != 13)
                e.Handled = true;
        }

        private void cmbSPTA_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbTransactionType.SelectedItem != null)
            {
                DataRowView dtRowViewTransType = cmbTransactionType.SelectedItem as DataRowView;
                PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSet.DL_WB_TRANSTYPE_INRow dtRowTransType = dtRowViewTransType.Row as PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSet.DL_WB_TRANSTYPE_INRow;

                if (dtRowTransType.REFTYPE == 1 && this.IsNewData)
                {
                    DataRowView dtRowViewSPTA = cmbSPTA.SelectedItem as DataRowView;
                    if (dtRowViewSPTA != null)
                    {
                        PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSet.DL_WB_SPTARow _dtRow_SPTA = dtRowViewSPTA.Row as PR_WEIGHTBRIDGE.Data.WB_MASTER_DataSet.DL_WB_SPTARow;

                        int _intVendor = 0;
                        if (int.TryParse(_dtRow_SPTA.SUPPLIER, out _intVendor))
                        {
                            cmbSupplierCode.SelectedValue = _intVendor.ToString();
                            cmbSupplier.SelectedValue = _intVendor.ToString();
                        }
                        else
                        {
                            cmbSupplierCode.SelectedValue = _dtRow_SPTA.SUPPLIER;
                            cmbSupplier.SelectedValue = _dtRow_SPTA.SUPPLIER;
                        }

                        int _intEstate = 0;
                        if (int.TryParse(_dtRow_SPTA.ESTATE, out _intEstate))
                        {
                            cmbEstateCode.SelectedValue = _intEstate.ToString();
                            cmbEstate.SelectedValue = _intEstate.ToString();
                        }
                        else
                        {
                            cmbEstateCode.SelectedValue = _dtRow_SPTA.ESTATE;
                            cmbEstate.SelectedValue = _dtRow_SPTA.ESTATE;
                        }



                        if (this.IsNewData)
                        {
                            this.WB_IN_DataSet.DL_WB_IN_DOC.Clear();
                            this.DL_WB_IN_DOCBindingSource.AddNew();
                            DataRowView _drv = this.DL_WB_IN_DOCBindingSource.Current as DataRowView;
                            WB_IN_DataSet.DL_WB_IN_DOCRow _row = _drv.Row as WB_IN_DataSet.DL_WB_IN_DOCRow;
                            _row.REFDOC = _dtRow_SPTA.SPTA_NUMBER;
                            this.DL_WB_IN_DOCBindingSource.EndEdit();
                        }
                    }
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {

            this.Close();
        }

        private void cmbEstate_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DL_WB_IN_REGbindingSource.Current != null)
            {
                //if (string.IsNullOrEmpty(((DL_WB_IN_REGbindingSource.Current as DataRowView).Row as Data.WB_IN_DataSet.DL_WB_IN_REGRow).SUPPLIER))
                //{
                if (sender == cmbEstate)
                {
                    if (cmbEstate.SelectedValue != null)
                    {
                        cmbEstate.DataBindings["SelectedValue"].WriteValue();
                        cmbEstateCode.DataBindings["SelectedValue"].ReadValue();
                    }
                }
                else if (sender == cmbEstateCode)
                {
                    if (cmbEstateCode.SelectedValue != null)
                    {
                        cmbEstateCode.DataBindings["SelectedValue"].WriteValue();
                        cmbEstate.DataBindings["SelectedValue"].ReadValue();
                    }
                }
                //}
            }
        }

        private void dgvDocument_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void PrintLabelANDBruto()
        {
            //YS 20110617
            if (PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.LabelPrintAfterWBOUT)
            {
                if (string.IsNullOrEmpty(PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.LabelPrinterName.Trim()))
                {
                    MessageBox.Show(this, string.Format("Cannot print label, printer name have not been setup", PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.LabelPrinterName), this.ParentForm.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                else if (!Business.Print.clsPrintHelper.PrinterExists(PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.LabelPrinterName))
                {
                    MessageBox.Show(this, string.Format("Cannot print label, cannot find printer with name \"{0}\"", PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.LabelPrinterName), this.ParentForm.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            if (PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.BrutoPrintAfterWBOUT)
            {
                if (string.IsNullOrEmpty(PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.BrutoPrinterName.Trim()))
                {
                    MessageBox.Show(this, string.Format("Cannot print bruto, printer name have not been setup", PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.LabelPrinterName), this.ParentForm.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                else if (!Business.Print.clsPrintHelper.PrinterExists(PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.BrutoPrinterName))
                {
                    MessageBox.Show(this, string.Format("Cannot print bruto, cannot find printer with name \"{0}\"", PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.BrutoPrinterName), this.ParentForm.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

            }
            if (this.DataTimbang != null && !this.DataTimbang.IsTIMBANG1Null())
            {
                PR_WEIGHTBRIDGE.Application.Business.Print.clsPrintHelper.Paper _paper = PR_WEIGHTBRIDGE.Application.Business.Print.clsPrintHelper.Paper.PLAIN;
                //LABEL
                //YS 20110617
                if (PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.LabelPrintAfterWBOUT)
                {
                    Microsoft.Reporting.WinForms.LocalReport _ticket = new Microsoft.Reporting.WinForms.LocalReport();
                    Business.Print.clsPrintHelper.LoadTebuLabel(_ticket
                        , _paper
                        , this.COMPCODE
                        , this.WBNUM
                        , this.YEAR
                        , 0
                        );
                    Utility.clsLabelPrintDocument _printDocument = new PR_WEIGHTBRIDGE.Utility.clsLabelPrintDocument(_ticket);
                    _printDocument.PrinterSettings.PrinterName = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.LabelPrinterName;
                    if (PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.UsePrintPreviewLabel)
                    {
                        PrintPreviewDialog _preview_dialog = new PrintPreviewDialog();
                        _preview_dialog.Document = _printDocument;
                        _preview_dialog.ShowDialog(this);
                    }
                    else
                    {
                        _printDocument.Print();
                    }
                }

                //BRUTO
                //YS 20110617
                if (PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.BrutoPrintAfterWBOUT)
                {
                    Microsoft.Reporting.WinForms.LocalReport _bruto = new Microsoft.Reporting.WinForms.LocalReport();
                    Business.Print.clsPrintHelper.LoadTebuBruto(_bruto
                        , _paper
                        , this.COMPCODE
                        , this.WBNUM
                        , this.YEAR
                        , 0, this.DataTimbang.TIMBANG1,
                        this.DataTimbang.WBDATE1.ToString("dd-MMM-yyyy hh:mm")
                        );
                    Utility.clsReportPrintDocument _brutoDocument = new PR_WEIGHTBRIDGE.Utility.clsReportPrintDocument(_bruto);
                    //YS 20110617 possibility of misuse variable
                    //_printDocument.PrinterSettings.PrinterName = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.BrutoPrinterName;
                    _brutoDocument.PrinterSettings.PrinterName = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.BrutoPrinterName;

                    if (PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.UsePrintPreviewBruto)
                    {
                        PrintPreviewDialog _preview_dialog = new PrintPreviewDialog();
                        _preview_dialog.Document = _brutoDocument;
                        _preview_dialog.ShowDialog(this);
                    }
                    else
                    {
                        _brutoDocument.Print();
                    }
                }
            }
        }

        bool tokenStart = true;
        string str = "";
        bool isScanned = false;
        private void txbDetailNote_KeyPress(object sender, KeyPressEventArgs e)
        {
            tokenStart = false;
            if (!tokenStart)
                str += e.KeyChar;
            //str = "3522B011799002;BM8458LA;AFRI ANTONI;400084";

            if (str.Length >= 13)
            {
                if (str.Contains(';'))
                {
                    isScanned = true;
                }

                //byte[] arrbyte = Encoding.ASCII.GetBytes(str);
                //if (arrbyte[16] == 59) //17th character is semicolon
                //    isScanned = true;
            }

            str = Regex.Replace(str, @"\t|\n|\r", "");
            str = ReplaceBackspace(str);

            if ((e.KeyChar == (char)13) && (isScanned)) //this mean 'Enter' pressed & string contains semicolon (;)
            {
                tokenStart = true;
                string[] arrStr = str.Split(';');

                string refDoc = string.Empty;
                string vehicleNo = string.Empty;
                string driverName = string.Empty;
                string supplierCode = string.Empty;
                string VehicleTypeCode = string.Empty;
                string sptaDate = string.Empty;
                string estateCode = string.Empty;
                string divisionCode = string.Empty;
                string runningAccount = string.Empty;


                for (int i = 0; i < arrStr.Length; i++)
                {
                    switch (i)
                    {
                        case 0:
                            refDoc = arrStr[i];
                            break;
                        case 1:
                            vehicleNo = arrStr[i];
                            break;
                        case 2:
                            driverName = arrStr[i];
                            break;
                        case 3:
                            supplierCode = arrStr[i];
                            break;
                        case 4:
                            VehicleTypeCode = arrStr[i];
                            break;
                        case 5:
                            sptaDate = arrStr[i];
                            break;
                        case 6:
                            estateCode = arrStr[i];
                            break;
                        case 7:
                            divisionCode = arrStr[i];
                            break;
                        case 8:
                            runningAccount = arrStr[i];
                            break;
                        default:
                            break;
                    }
                }

                if (arrStr != null && arrStr.Length > 2)
                {
                    this.txbVehicleNo.Text = vehicleNo;
                    this.txbDriverName.Text = driverName;
                    this.txbVehicleCode.Text = VehicleTypeCode;

                    txbVehicleNo.DataBindings["Text"].WriteValue();
                    txbDriverName.DataBindings["Text"].WriteValue();

                    var vendors = Data.clsMasterData.DL_WB_VENDORDataTable;
                    var vendor = vendors.Select("SUPPLIER = '" + supplierCode.TrimStart('0') + "'").FirstOrDefault();
                    if (vendor != null)
                    {
                        cmbSupplierCode.SelectedValue = vendor["SUPPLIER"];
                        var trxType = vendor["TRX_TYPE"];
                        var transactionTypes = Data.clsMasterData.DL_WB_TRANSTYPE_INDataTable;
                        var transactionType = transactionTypes.Select(" TRANSTYPE = '" + trxType + "' AND ACTIVE = true").FirstOrDefault();
                        if (transactionType != null)
                        {
                            cmbTransactionType.SelectedValue = transactionType["TRANSTYPE"];
                        }
                    }
                    else
                    {

                    }

                    WB_IN_DataSet dtSet = (WB_IN_DataSet)DL_WB_IN_DOCBindingSource.DataSource;
                    DataTable table = dtSet.Tables["DL_WB_IN_DOC"];
                    int countRow = table.Rows.Count;
                    if (countRow > 0)
                    {
                        for (int i = 0; i < countRow; i++)
                        {
                            if (table.Rows[i]["REFDOC"].ToString() == refDoc)
                            {
                                MessageBox.Show("Reference Doc already scanned", "Ref Doc", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                str = "";
                                this.txbDetailNote.Text = "";
                                this.txbDetailNote.Focus();

                                return;
                            }
                        }
                    }

                    DataRow rowi = table.NewRow();
                    rowi["YEAR"] = this.YEAR;
                    rowi["REFDOC"] = refDoc;
                    rowi["WBNUM"] = "";
                    rowi["COMPCODE"] = txbCompanyID.Text;
                    if (sptaDate != String.Empty)
                    {
                        DateTime resultDate = new DateTime();
                        if (DateTime.TryParseExact(sptaDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out resultDate))
                        {
                            rowi["REFDATE"] = resultDate;
                        }
                        else
                        {
                            // MessageBox.Show("Ref Date format is wrong.", "Ref Date", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    rowi["ESTATE"] = estateCode;
                    rowi["DIVISI"] = divisionCode;
                    rowi["RUNNINGACCOUNT"] = runningAccount;

                    table.Rows.Add(rowi);
                }

                str = "";
                this.txbDetailNote.Text = "";
                this.txbDetailNote.Focus();
            }
        }

        public string ReplaceBackspace(string hasBackspace)
        {
            if (string.IsNullOrEmpty(hasBackspace))
                return hasBackspace;

            StringBuilder result = new StringBuilder(hasBackspace.Length);
            foreach (char c in hasBackspace)
            {
                if (c == '\b')
                {
                    if (result.Length > 0)
                        result.Length--;
                }
                else
                {
                    result.Append(c);
                }
            }
            return result.ToString();
        }



        private void txtQRCode_KeyPress(object sender, KeyPressEventArgs e)
        {

            tokenStart = false;

            string _qrESTATE = string.Empty;
            string _qrDivisi = string.Empty;
            string _qrRunningAccount = string.Empty;
            string _qrClerk = string.Empty;
            DateTime _qrRefDate;
            bool _qrUserQrCode = false;



            if (!tokenStart)
                str += e.KeyChar;

            if (scanNfcBtn.Visible == true) // untuk memasukan data text dari nfc reader ke str. Kenapa ada fungsi ini karena jika dari QR Code data yang dibaca per huruf yang di input oleh alat QR Code. Sedangkan fitur NFC ini langsung replace data textbox. by jerry 09-06-2022
                str = txtQRCode.Text;

            if (str.Length >= 13)
            {
                if (str.Contains(';'))
                {
                    isScanned = true;
                }
            }

            str = Regex.Replace(str, @"\t|\n|\r", "");
            str = ReplaceBackspace(str);

            if ((e.KeyChar == (char)13) && (isScanned)) //this mean 'Enter' pressed & string contains semicolon (;)
            //if ((e.KeyChar == (char)13)) //this mean 'Enter' pressed & string contains semicolon (;)
            {
                tokenStart = true;
                string[] arrStr = str.Split(';');

                string refDoc = string.Empty;
                string vehicleNo = string.Empty;
                string driverName = string.Empty;
                string supplierCode = string.Empty;
                string VehicleTypeCode = string.Empty;
                string sptaDate = string.Empty;
                string estateCode = string.Empty;
                string divisionCode = string.Empty;
                string runningAccount = string.Empty;
                string clerk = string.Empty;

                //Add perubahan QR baru 250620

                int HeaderSeparator = PR_WEIGHTBRIDGE.Application.Business.WeightBridge.Config.clsWBConfiguration.WBConfig.LimitFieldHeader;
                int LimitBarCode;

                if (arrStr.Length < HeaderSeparator)
                {
                    LimitBarCode = arrStr.Length;
                }
                else
                {
                    LimitBarCode = HeaderSeparator;
                }

                for (int i = 0; i < LimitBarCode; i++)
                {
                    switch (i)
                    {
                        case 0:
                            refDoc = arrStr[i];
                            break;
                        case 1:
                            vehicleNo = arrStr[i];
                            break;
                        case 2:
                            driverName = arrStr[i];
                            break;
                        case 3:
                            supplierCode = arrStr[i];
                            break;
                        case 4:
                            VehicleTypeCode = arrStr[i];
                            break;
                        case 5:
                            sptaDate = arrStr[i];
                            break;
                        case 6:
                            estateCode = arrStr[i];
                            break;
                        case 7:
                            divisionCode = arrStr[i];
                            break;
                        case 8:
                            clerk = arrStr[i];
                            break;
                        case 9:
                            runningAccount = arrStr[i];
                            break;
                        default:
                            break;
                    }
                }

                PR_WEIGHTBRIDGE.Data.WB_IN_DataSet.DL_WB_IN_DOC_ITEMRow item_row = this.WB_IN_DataSet.DL_WB_IN_DOC_ITEM.NewDL_WB_IN_DOC_ITEMRow();
                var NewArr = arrStr.Skip(HeaderSeparator);
                string[] NewArrFinal = NewArr.ToArray();

                if (NewArrFinal.Count() > 0 && arrStr.Length > HeaderSeparator)
                {
                    for (int i = 0; i < NewArrFinal.Count(); i++)
                    {
                        var HasilMod = i % 6;
                        if (HasilMod == 0)
                        {
                            item_row = this.WB_IN_DataSet.DL_WB_IN_DOC_ITEM.NewDL_WB_IN_DOC_ITEMRow();
                        }
                        switch (HasilMod)
                        {
                            case 0:
                                item_row.LINENUM = Int32.Parse(NewArrFinal[i]);
                                break;
                            case 1:
                                item_row.BLOCK = NewArrFinal[i];
                                break;
                            case 2:
                                DateTime resultHrvDate = new DateTime();
                                if (NewArrFinal[i] != String.Empty)
                                {
                                    if (DateTime.TryParseExact(NewArrFinal[i], "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out resultHrvDate))
                                    {
                                        item_row.HARVESTDATE = resultHrvDate;
                                    }
                                    else
                                    {
                                        item_row.HARVESTDATE = DateTime.MaxValue;
                                    }
                                }
                                else
                                {
                                    item_row.HARVESTDATE = DateTime.MaxValue;
                                }
                                break;
                            case 3:
                                item_row.JANJANG = decimal.Parse(NewArrFinal[i]);
                                break;

                            case 4:
                                item_row.LOOSEFRUIT = decimal.Parse(NewArrFinal[i]);
                                break;

                            case 5:
                                var rnd = new Random(DateTime.Now.Millisecond);
                                int ticks = rnd.Next(0, 3000);
                                item_row.SPBS = refDoc;
                                item_row.WBNUM = this.WBNUM;
                                //item_row.BJR = decimal.Parse(NewArrFinal[i].Replace(".", ","));
                                item_row.BJR = decimal.Parse(NewArrFinal[i]);
                                item_row.CREATEDBY = Setting.UserId;
                                item_row.CREATEDDATE = DateTime.Now;
                                item_row.LASTUPDATEDBY = Setting.UserId;
                                item_row.LASTUPDATEDDATE = DateTime.Now;
                                item_row.YEAR = DateTime.Now.Year.ToString();
                                this.WB_IN_DataSet.DL_WB_IN_DOC_ITEM.AddDL_WB_IN_DOC_ITEMRow(item_row);
                                break;

                            default:
                                break;
                        }
                    }
                }
                //End Add perubahan QR baru 250620

                if (arrStr != null && arrStr.Length > 2)
                {
                    this.txbVehicleNo.Text = vehicleNo;
                    this.txbDriverName.Text = driverName;
                    this.txbVehicleCode.Text = VehicleTypeCode;

                    txbVehicleNo.DataBindings["Text"].WriteValue();
                    txbDriverName.DataBindings["Text"].WriteValue();

                    var vendors = Data.clsMasterData.DL_WB_VENDORDataTable;
                    var vendor = vendors.Select("SUPPLIER = '" + supplierCode.TrimStart('0') + "'").FirstOrDefault();
                    if (vendor != null)
                    {
                        var trxType = vendor["TRX_TYPE"];
                        var transactionTypes = Data.clsMasterData.DL_WB_TRANSTYPE_INDataTable;
                        var transactionType = transactionTypes.Select(" TRANSTYPE = '" + trxType + "' AND ACTIVE = true").FirstOrDefault();
                        if (transactionType != null)
                        {
                            cmbTransactionType.SelectedValue = transactionType["TRANSTYPE"];
                            cmbTransactionType.DataBindings["SelectedValue"].WriteValue();
                            cmbSupplierCode.SelectedValue = vendor["SUPPLIER"];
                        }
                    }
                    else
                    {

                    }

                    WB_IN_DataSet dtSet = (WB_IN_DataSet)DL_WB_IN_DOCBindingSource.DataSource;
                    DataTable table = dtSet.Tables["DL_WB_IN_DOC"];
                    int countRow = table.Rows.Count;
                    if (countRow > 0)
                    {
                        for (int i = 0; i < countRow; i++)
                        {
                            if (table.Rows[i]["REFDOC"].ToString() == refDoc)
                            {
                                MessageBox.Show("Reference Doc already scanned", "Ref Doc", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                str = "";
                                this.txtQRCode.Text = "";
                                this.txtQRCode.Focus();

                                return;
                            }
                        }
                    }

                    DateTime sptaDateResult = new DateTime();
                    if (sptaDate != String.Empty)
                    {
                        if (DateTime.TryParseExact(sptaDate, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out sptaDateResult))
                        {
                            sptaDate = sptaDateResult.ToString();
                            this._qrRefDate = sptaDateResult;
                        }
                        else
                        {
                            this._qrRefDate = DateTime.MaxValue;
                            sptaDate = DateTime.MaxValue.ToString();
                            // MessageBox.Show("Ref Date format is wrong.", "Ref Date", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        this._qrRefDate = DateTime.MaxValue;
                        sptaDate = DateTime.MaxValue.ToString();
                    }

                    DataRow rowi = table.NewRow();
                    rowi["YEAR"] = this.YEAR;
                    rowi["REFDOC"] = refDoc;
                    rowi["WBNUM"] = "";
                    rowi["COMPCODE"] = txbCompanyID.Text;
                    rowi["USEQRCODE"] = true;

                    //Add perubahan QR baru 250620
                    rowi["CLERK"] = clerk;
                    //End Add perubahan QR baru 250620


                    rowi["ESTATE"] = estateCode;
                    rowi["DIVISI"] = divisionCode;
                    rowi["RUNNINGACCOUNT"] = runningAccount;

                    //Add perubahan QR baru 250620
                    //Fill Qr if new reff doc;
                    this._qrRunningAccount = runningAccount;
                    this._qrESTATE = estateCode;
                    this._qrClerk = clerk;
                    this._qrDivisi = divisionCode;
                    this._qrUserQrCode = true;
                    //End fill Qr if new reff doc;
                    table.Rows.Add(rowi);
                    DataRowView _drv = this.DL_WB_IN_DOCBindingSource.Current as DataRowView;
                    WB_IN_DataSet.DL_WB_IN_DOCRow _row = _drv.Row as WB_IN_DataSet.DL_WB_IN_DOCRow;
                    _row.REFDATE = this._qrRefDate;
                    this.DL_WB_IN_DOCBindingSource.EndEdit();
                }
                //End Add perubahan QR baru 250620
                str = "";
                this.txtQRCode.Text = "";
                this.txtQRCode.Focus();
            }
        }

        private void txbWeightInEx_DoubleClick(object sender, EventArgs e)
        {
            this.txbWeightInEx.Visible = false;
        }

        public void GenerateQrcode()
        {
            string _REFDOC = "";
            byte _rowCnt = 0;
            StringBuilder _strBuilder = new StringBuilder();
            foreach (Data.WB_IN_DataSet.DL_WB_IN_DOCRow dtRowDoc in this.WB_IN_DataSet.DL_WB_IN_DOC)
            {
                if (dtRowDoc.RowState != DataRowState.Deleted && _rowCnt < 5)
                {
                    _REFDOC += ";" + dtRowDoc.REFDOC;
                    _rowCnt++;
                }
            }

            //string _QRCodeData = string.Concat(DataRegistrasi.WBNUM + ";" + DataRegistrasi.TRANSTYPE + ";" + DataRegistrasi.WBDATE.ToString("yyyy-MM-dd") + ";" + DataTimbang.WBDATE1.ToString("HH:mm:ss") + ";" + DataTimbang.WBDATE2.ToString("HH:mm:ss") + ";" + Convert.ToInt32(DataTimbang.NET).ToString() + ";0" + _REFDOC);

            //writeToNfc = _QRCodeData;
            //writeNfcAccess = true;

            //if (scanNfcBtn.Visible = false) //Jika tidak ada alat NFC yang terkoneksi maka muncul QR Code di layar
            //{
            string _QRCodeData = string.Concat(DataRegistrasi.WBNUM + ";" + DataRegistrasi.TRANSTYPE + ";" + DataRegistrasi.WBDATE.ToString("yyyy-MM-dd") + ";" + DataTimbang.WBDATE1.ToString("HH:mm:ss") + ";" + DataTimbang.WBDATE2.ToString("HH:mm:ss") + ";" + Convert.ToInt32(DataTimbang.NET).ToString() + ";0" + _REFDOC); //// pindah ke atas diluar if

            frmQRCode frmQRCode = new frmQRCode();
            frmQRCode.QRCodeData = _QRCodeData;// "3551NR20017000;TBS3;2020-09-08;09:33:00;09:35:00;30;0;8130I012099003;8130I012099004;8130I012099005";

            frmQRCode.ShowDialog(this);
            frmQRCode.Dispose();
            //}
            //else if (scanNfcBtn.Visible = true) {
            //   // munculkan dialog untuk meletakan nfc disini
            //}

        }

        private void lblWeightIn_DoubleClick(object sender, EventArgs e)
        {

        }

        private void tableLayoutPanel6_Paint(object sender, PaintEventArgs e)
        {

        }

        async void recordBtn_Click(object sender, EventArgs e)
        {
            // Membaca nilai dari app.config
            string cameraUrl = ConfigurationManager.AppSettings["CameraUrl"];
            string cameraUsername = ConfigurationManager.AppSettings["CameraUsername"];
            string cameraPassword = ConfigurationManager.AppSettings["CameraPassword"];

            // Atur ProgressBar untuk memulai proses loading
            progressBar1.Style = ProgressBarStyle.Marquee;
            progressBar1.MarqueeAnimationSpeed = 30;

            try
            {
                Detection detection = new Detection(true);
                var result = await detection.ProcessFromCamera(cameraUrl, cameraUsername, cameraPassword);

                // Memeriksa apakah result memiliki nilai
                if (result != null && result.Count > 0 && !string.IsNullOrEmpty(result[0].number))
                {
                    var n = result[0].number;
                    var p = result[0].image;
                    var pf = result[0].image_full;

                    // Mengatur mode gambar untuk menyesuaikan ukuran PictureBox
                    pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;

                    string base64String = string.Format($"data:image/png;base64,{p}");

                    // Menghapus prefix data:image jika ada
                    if (base64String.Contains(","))
                    {
                        base64String = base64String.Split(',')[1];
                    }

                    // Konversi Base64 string menjadi array byte
                    byte[] imageBytes = Convert.FromBase64String(base64String);

                    // Menggunakan array byte untuk membuat Image
                    using (var ms = new MemoryStream(imageBytes))
                    {
                        pictureBox1.Image = Image.FromStream(ms);
                    }

                    // Mengatur nilai enabled untuk btnSave berdasarkan nilai n
                    this.btnSave.Enabled = true;
                    textBox1.Text = n;
                    label6.Text = p;
                    label7.Text = pf;

                    //True jika ada
                    if (this.txbVehicleNo.Text == n)
                    {
                        this.label5.Visible = false;
                    }
                    else if (this.txbVehicleNo.Text != n)
                    {
                        this.label5.Visible = true;
                    }

                }
                else
                {
                    // Jika result kosong atau n tidak memiliki nilai
                    this.btnSave.Enabled = false;
                    textBox1.Text = "No data available";
                }
            }
            finally
            {
                // Setel ProgressBar ke 100 saat operasi selesai
                progressBar1.Style = ProgressBarStyle.Blocks;
                progressBar1.MarqueeAnimationSpeed = 0;
                progressBar1.Value = 100;
            }
        }
    }
}
