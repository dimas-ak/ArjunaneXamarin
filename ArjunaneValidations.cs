using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Xamarin.Forms;

namespace ArjunaneXamarin
{
    public class ArjunaneValidations
    {
        private readonly Dictionary<string, string> form_post        = new Dictionary<string, string>();

        private List<form_fields> fields = new List<form_fields>();
        // digunakan untuk menyimpan data field dari entry, editor dan picker
        // dimana berisi key (string) dan error (string)
        private readonly List<form_field_error> field_error          = new List<form_field_error>();

        // guna menampilkan form error
        // contoh form_error("email")
        private readonly Dictionary<string, string> form_error                   = new Dictionary<string, string>();

        // guna menyimpan custom message untuk form_error
        // kalau di CI seperti set_error_delimiters
        private Dictionary<string, string> form_error_message           = new Dictionary<string, string>();

        private readonly Dictionary<string, string> form_error_message_default = new Dictionary<string, string>
        {
            { "ip",                 "{field} tidak valid." },
            { "gt",                 "{field} harus lebih besar dari {param}." }, //Greater Than
            { "lt",                 "{field} harus lebih kecil dari {param}." }, //Lower Than
            { "gte",                "{field} harus lebih besar dari sama dengan {param}." }, //Greater Than or Equal
            { "lte",                "{field} harus lebih kecil dari sama dengan {param}." }, //Lower Than or Equal
            { "max",                "Maksimal Angka dari {field} ialah {param}." },
            { "min",                "Minimal Angka dari {field} ialah {param}." },
            { "url",                "{field} tidak valid." },
            { "ipv4",               "{field} tidak valid." },
            { "ipv6",               "{field} tidak valid." },
            { "json",               "{field} tidak valid." },
            { "name",               "{field} tidak valid." },
            { "same",               "{field} tidak sama dengan {param}." },
            { "email",              "{field} tidak valid." },
            { "regex",              "{field} tidak valid." },
            { "alpha",              "{field} hanya boleh diisi dengan Alphabet." },
            { "integer",            "{field} tidak valid." },
            { "numeric",            "{field} tidak valid." },
            { "boolean",            "{field} tidak valid." },
            { "required",           "{field} harap diisi." },
            { "in_array",           "{field} tidak valid." },
            { "ends_with",          "{field} harus diakhiri dengan {param}." },
            { "different",          "{field} tidak boleh sama dengan {param}." },
            { "not_regex",          "{field} tidak valid." },
            { "max_length",         "Maksimal Karakter untuk {field} ialah {param}." },
            { "min_length",         "Minimal Karakter untuk {field} ialah {param}." },
            { "alpha_dash",         "{field} hanya boleh diisi dengan Alphabet dan Garis." },
            { "starts_with",        "{field} harus diawali dengan {param}." },
            { "required_if",        "{field} harus diisi." },
            { "not_in_array",       "{field} tidak valid." },
            { "alpha_numeric",      "{field} hanya boleh diisi dengan Alphabet dan Angka." },
            { "alpha_numeric_dash", "{field} hanya boleh diisi dengan Alphabet, Angka dan Garis." },
        };

        // digunakan apabila ExceptKey maupun OnlyKey dijalankan
        // RunOnlyKey RunExceptKey
        List<form_fields> tmp_fields = new List<form_fields>();

        Execute ex;

        private static bool is_fails = false;
        public class form_fields
        {
            public Entry entry          { get; set; } = null;
            public Editor editor        { get; set; } = null;
            public Picker picker        { get; set; } = null;
            public string key           { get; set; }
            public string label         { get; set; }
            public string validations   { get; set; }
            public List<string> list = new List<string>();
        }
        public class form_field_error
        {
            public string key   { get; set; }
            public string error { get; set; }
        }

        public ArjunaneValidations()
        {
            ResetForm();
        }

        public ArjunaneValidations FormReset()
        {
            ResetForm();
            return this;
        }

        private void ResetForm()
        {
            field_error.Clear();

            form_error.Clear();
            form_error_message.Clear();

            AddOverrideErrosMessage(OverrideErrorsMessage());
        }

        // untuk menambahkan/replace pesan/message validasi yang di inginkan User
        // melalui pembuatan Class baru dimana Class tersebut extends ke ArjunaneValidations
        private void AddOverrideErrosMessage(Dictionary<string, string> dict)
        {
            if (dict.Count == 0) return;

            foreach(var ini in dict)
            {
                if (form_error_message.ContainsKey(ini.Key)) form_error_message[ini.Key] = ini.Value;
                else form_error_message.Add(ini.Key, ini.Value);
            }
        }

        /// <summary>
        /// validations = Name of validation<br/><br/>
        /// message = Your message<br/>
        /// replace label regex is : {field} <br/>
        /// replace number (max,min,max_length,min_length,field) regex is : {param}
        /// </summary>
        /// <param name="validations"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public ArjunaneValidations SetErrorMessage(string validation, string message)
        {
            if(!form_error_message.ContainsKey(validation)) form_error_message.Add(validation, message);
            return this;
        }
        /// <summary>
        /// string name : your name (primary key)<br/>
        /// Entry : your entry <br/>
        /// string label : your label string<br/>
        /// string validations : your validations
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="label"></param>
        /// <param name="validations"></param>
        /// <returns></returns>
        public ArjunaneValidations SetRules(string name, Entry entry, string label, string validations)
        {
            var _entry = new form_fields()
            {
                key         = name,
                entry       = entry,
                label       = label,
                validations = validations
            };
            if (fields.Count != 0)
            {
                foreach (var ini in fields)
                {
                    if (ini.key == name) throw new ArgumentException("Duplicate key for 'name' Entry : " + name);
                }
            }

            var err = new form_field_error()
            {
                key = name,
                error = null
            };

            fields.Add(_entry);
            addFieldError(err);
            return this;
        }
        /// <summary>
        /// string name : your name (primary key)<br/>
        /// Editor : your editor <br/>
        /// string label : your label string<br/>
        /// string validations : your validations
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="label"></param>
        /// <param name="validations"></param>
        /// <returns></returns>
        public ArjunaneValidations SetRules(string name, Editor editor, string label, string validations)
        {
            var _editor = new form_fields()
            {
                key = name,
                editor = editor,
                label = label,
                validations = validations
            }; 
            
            if (fields.Count != 0)
            {
                foreach (var ini in fields)
                {
                    if (ini.key == name) throw new ArgumentException("Duplicate key for 'name' Editor : " + name, "name");
                }
            }

            var err = new form_field_error()
            {
                key = name,
                error = null
            };

            addFieldError(err);
            fields.Add(_editor);
            return this;
        }
        /// <summary>
        /// string name : your name (primary key)<br/>
        /// Picker : your picker <br/>
        /// string label : your label string <br/>
        /// list_picker : your List data
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="label"></param>
        /// <param name="validations"></param>
        /// <returns></returns>
        public ArjunaneValidations SetRules(string name, Picker picker, string label, string validations, List<string> list_picker = null)
        {
            var _picker = new form_fields()
            {
                key         = name,
                picker      = picker,
                validations = validations,
                label       = label
            };

            if (list_picker != null)
            {
                _picker.list = list_picker;
            }

            if (fields.Count != 0)
            {
                foreach (var ini in fields)
                {
                    if (ini.key == name) throw new ArgumentException("Duplicate key for 'name' Picker : " + name);
                }
            }

            var err = new form_field_error()
            {
                key = name,
                error = null
            };

            addFieldError(err);
            fields.Add(_picker);
            return this;
        }

        // untuk mengganti List (Picker) dari User
        public void ReplaceFormPickerList(string key, List<string> list_picker)
        {
            if(fields.Count != 0)
            {
                foreach (var ini in fields)
                {
                    if (ini.key == key)
                    {
                        ini.list.Clear();
                        ini.list = list_picker;
                        return;
                    }
                }
            }
        }

        // untuk menambahkan List (Picker) dari User
        public void InsertFormPickerList(string key, List<string> list_picker)
        {
            if (fields.Count != 0)
            {
                foreach (var ini in fields)
                {
                    if (ini.key == key)
                    {
                        foreach(var list in list_picker)
                        {
                            ini.list.Add(list);
                        }
                        return;
                    }
                }
            }
        }

        // untuk mendapatkan semua value dari input maupun picker
        // beserta Key nya
        // return Key dan Value
        public Dictionary<string, string> All(bool all_fields = false)
        {
            form_post.Clear();

            var _fields = all_fields ? fields : setFields();

            if(_fields.Count != 0)
            {
                foreach(var ini in _fields)
                {
                    if(ini.entry != null)
                    {
                        form_post.Add(ini.key, ini.entry.Text);
                    }
                    else if (ini.editor != null)
                    {
                        form_post.Add(ini.key, ini.editor.Text);
                    }
                    else if (ini.picker != null && ini.list.Count != 0)
                    {
                        form_post.Add(ini.key, ini.list[ini.picker.SelectedIndex]);
                    }
                }
            }
            return form_post;
        }

        // saat User melakukan typing pada Entry atau Editor
        public void OnInput(string key, Action<string> act)
        {
            bool is_find = false;
            
            if (fields.Count != 0)
            {
                foreach (var ini in fields)
                {
                    if (ini.key == key && ini.entry != null)
                    {
                        is_find = true;

                        ini.entry.TextChanged += (s, e) =>
                        {

                            var obj = (Entry)s;

                            form_error.Clear();

                            form_error.Add(ini.key, null);
                            InitErrors(obj.Text, ini.validations, ini.key, ini.label);

                            string msg = form_error[key] ?? null;
                            act(msg);
                        };
                        return;
                    }
                    else if (ini.key == key && ini.editor != null)
                    {
                        is_find = true;

                        ini.editor.TextChanged += (s, e) =>
                        {

                            var obj = (Editor)s;

                            form_error.Clear();

                            form_error.Add(ini.key, null);
                            InitErrors(obj.Text, ini.validations, ini.key, ini.label);

                            string msg = form_error[key] ?? null;
                            act(msg);
                        };
                        return;
                    }
                }
            }

            if (!is_find) throw new ArgumentException("Cannot find key for : " + key);

        }
        // untuk validasi jika ada duplikasi data key dari setiap Entry, Picker dan Editor
        private void addFieldError(form_field_error err)
        {
            try
            {
                field_error.Add(err);
            }
            catch(Exception)
            {
                throw new ArgumentException("Duplicate key for 'name' : " + err.key);
            }
        }

        /// <summary>
        /// is_enabled : bool <br/>
        /// true : fields are Enabled <br/>
        /// false : fields are Disabled<br/><br/>
        /// 
        /// all_fields : bool (default to False)<br/>
        /// true : get all fields <br/>
        /// false : get all_fields (Except or Only Key(s))
        /// </summary>
        /// <param name="is_enabled"></param>
        /// <param name="all_fields"></param>
        public void FormEnabled(bool is_enabled, bool all_fields = false)
        {
            var _fields = all_fields ? fields : setFields();

            if (_fields.Count != 0)
            {
                foreach (var ini in _fields)
                {
                    if(ini.entry != null)ini.entry.IsEnabled = is_enabled;
                    else if(ini.editor != null)ini.editor.IsEnabled = is_enabled;
                    else if (ini.picker != null)ini.picker.IsEnabled = is_enabled;
                }
            }
        }

        /// <summary>
        /// all_fields : bool (default to False)<br/>
        /// true : get all fields <br/>
        /// false : get all_fields (Except or Only Key(s))
        /// </summary>
        /// <param name="all_fields"></param>
        public void FormResetValue(bool all_fields = false)
        {
            var _fields = all_fields ? fields : setFields();

            if (_fields.Count != 0)
            {
                foreach (var ini in _fields)
                {
                    if (ini.entry != null) ini.entry.Text = "";
                    else if (ini.editor != null) ini.editor.Text = "";
                    else if (ini.picker != null) ini.picker.SelectedIndex = -1;
                }
            }
        }
        public void FormResetValueOnlyKey(params string[] OnlyKeys)
        {
            foreach (var ini in fields)
            {
                if (OnlyKeys.Contains(ini.key))
                {
                    if (ini.entry != null) ini.entry.Text = "";
                    else if (ini.editor != null) ini.editor.Text = "";
                    else if (ini.picker != null) ini.picker.SelectedIndex = -1;
                }
            }
        }
        public void FormResetValueExceptKey(params string[] ExceptKey)
        {
            foreach (var ini in fields)
            {
                if (!ExceptKey.Contains(ini.key))
                {
                    if (ini.entry != null) ini.entry.Text = "";
                    else if (ini.editor != null) ini.editor.Text = "";
                    else if (ini.picker != null) ini.picker.SelectedIndex = -1;
                }
            }
        }

        // digunakan untuk menjalankan Validasi sesuai dengan Key (HANYA) dari OnlyKeys
        public bool RunOnlyKeys(params string[] OnlyKeys)
        {
            tmp_fields.Clear();
            foreach (var ini in fields)
            {
                if (OnlyKeys.Contains(ini.key)) tmp_fields.Add(ini);
            }
            return _Run(tmp_fields);
        }

        // digunakan untuk menjalankan Validasi sesuai dengan Key (KECUALI) dari ExceptKey
        public bool RunExceptKeys(params string[] ExceptKeys)
        {
            tmp_fields.Clear();
            foreach (var ini in fields)
            {
                if (!ExceptKeys.Contains(ini.key)) tmp_fields.Add(ini);
            }
            return _Run(tmp_fields);
        }

        public bool Run()
        {
            return _Run(fields);
        }

        // jika tmp_fields (OnlyKey/ExceptKey) tidak kosong maka tmp_fields digunakan
        // jika kosong maka fields yang digunakan
        private List<form_fields> setFields()
        {
            return tmp_fields.Count > 0 ? tmp_fields : fields;
        }

        private bool _Run(List<form_fields> _fields)
        {
            is_fails = false;

            ex = new Execute(_fields);

            form_error.Clear();

            if (_fields.Count != 0)
            {
                foreach (var ini in _fields)
                {
                    if (ini.entry != null)
                    {
                        form_error.Add(ini.key, null);
                        InitErrors(ini.entry.Text, ini.validations, ini.key, ini.label);
                    }
                    else if (ini.editor != null)
                    {
                        form_error.Add(ini.key, null);
                        InitErrors(ini.editor.Text, ini.validations, ini.key, ini.label);
                    }
                    else if (ini.picker != null)
                    {
                        string value = ini.picker.SelectedIndex != -1 ? ini.list[ini.picker.SelectedIndex] : "";
                        form_error.Add(ini.key, null);
                        // InitErrorsSelected(ini.picker, ini.key, ini.label);
                        InitErrors(value, ini.validations, ini.key, ini.label);
                    }
                }
            }

            return is_fails != true;
        }

        public bool Fails()
        {
            return is_fails;
        }

        public Dictionary<string, string> Errors()
        {
            return form_error;
        }

        // untuk validasi picker
        private void InitErrorsSelected(Picker picker, string key, string label)
        {
            if(picker.SelectedIndex == -1)
            {
                string msg;

                // custom pesan error dari user
                if (form_error_message.Count > 0 && form_error_message.ContainsKey("required")) msg = replaceAttribute(form_error_message["required"], label);
                // default pesan
                else msg = replaceAttribute(form_error_message_default["required"], label);

                form_error[key] = msg;
                is_fails = true;
            }
        }

        // untuk validasi Entry dan Editor
        private void InitErrors(string value, string validations, string key, string label)
        {
            // split setiap validasi yang di masukkan
            var validation = validations.Split('|');

            foreach (var ini in validation)
            {
                // jika di validasi ada "nullable"
                // nullable digunakan untuk mengabaikan field jika kosong
                // dan apa bila field tidak kosong, maka akan dialihkan ke validasi selanjutnya
                if(ini == "nullable")
                {
                    Type type = typeof(Execute);

                    MethodInfo method = type.GetMethod(Capitalize("required"));

                    bool is_null = (bool)method.Invoke(ex, new object[] { value });

                    if (is_null) return;

                    continue;
                }

                // jika ada validasi yang memerlukan parameter angka atau titik dua (max, min, max_length, min_length)
                if (isNumValidation(ini))
                {
                    var split       = ini.Split(':');
                    // misal sebelum di split ialah max_length:20
                    string valid    = split[0]; // mendapatkan nama validasi (contoh max_length)
                    string _param   = split[1]; // mendapatkan parameter (contoh 20)

                    // ini untuk menentukan apakah parameter berupa field
                    // contoh same:password
                    // jadi parameter kedua ialah password
                    bool is_double  = double.TryParse(_param, out _);

                    string param    = null;
                    // parameter yang akan ditampilkan {param} ialah angka (contoh 20)
                    if (is_double) param = _param;
                    // parameter yang akan ditampilkan {param} ialah label dari key (contoh password)
                    else
                    {
                        foreach(var i in fields)
                        {
                            if (i.key == _param)
                            {
                                param = i.label;
                                break;
                            }
                        }
                    }

                    if (isValidationError(value, valid, _param))
                    {
                        string msg;

                        // custom pesan error dari user
                        if (form_error_message.Count > 0 && form_error_message.ContainsKey(valid)) msg = replaceAttribute(form_error_message[valid], label, param);
                        // default pesan
                        else msg = replaceAttribute(form_error_message_default[valid], label, param);

                        form_error[key] = msg;
                        is_fails        = true;
                        return;
                    }
                }
                else
                {
                    if (isValidationError(value, ini))
                    {
                        string msg;

                        // custom pesan error dari user
                        if (form_error_message.Count > 0 && form_error_message.ContainsKey(ini)) msg = replaceAttribute(form_error_message[ini], label);
                        // default pesan
                        else msg = replaceAttribute(form_error_message_default[ini], label);

                        form_error[key] = msg;
                        is_fails        = true;
                        return;
                    }
                }
            }
        }

        public virtual Dictionary<string, string> OverrideErrorsMessage() 
        {
            return form_error_message;
        }

        // mengeksekusi setiap validasi yang diberikan
        // dimana setiap hasil yang bernilai true maka error (masih dalam tahahp validasi)
        // sebaliknya jika return nya false, berarti inputan tidak ada yang bermasalah
        private bool isValidationError(string value, string validation, string param = null)
        {
            Type type = typeof(Execute);

            MethodInfo method = type.GetMethod(Capitalize(validation));

            if (method != null)
            {
                return (param != null) ? (bool)method.Invoke(ex, new object[] { value, param }) : (bool)method.Invoke(ex, new object[] { value });
            }

            return false;
        }

        // untuk membuat huruf pertama besar
        private string Capitalize(string value)
        {
            var strs = value.Split('_');
            string result = null;
            foreach (var i in strs)
            {
                result += char.ToUpper(i[0]) + i.Substring(1);
            }
            return "isError" + result;
        }
        private bool isNumValidation(string validation)
        {
            return validation.Split(':').Length > 1;
        }

        // untuk merubah {field} menjadi Label sesuai dengan yang di inginkan User
        // untuk merubah {param} menjadi Param sesuai dengan yang di inginkan User
        private string replaceAttribute(string value, string label, string param = null)
        {
            string regex = value.Replace("{field}", label);
            if (param != null) regex = regex.Replace("{param}", param);
            return regex;
        }

        public class Execute
        {
            readonly List<form_fields> fields = new List<form_fields>();
            public Execute(List<form_fields> list)
            {
                fields = list;
            }
            public bool isErrorEmail(string email)
            {
                try
                {
                    var addr = new System.Net.Mail.MailAddress(email);
                    return addr.Address != email;
                }
                catch (Exception)
                {
                    return true;
                }
            }
            public bool isErrorRequired(string value) { return string.IsNullOrEmpty(value); }
            public bool isErrorRequiredIf(string value, string param) 
            { 
                foreach(var ini in fields)
                {
                    if      (
                        ini.entry != null && 
                        !string.IsNullOrEmpty(ini.entry.Text) && 
                        ini.key == param && 
                        string.IsNullOrEmpty(value)) return true;

                    else if (ini.editor != null && 
                        !string.IsNullOrEmpty(ini.editor.Text) && 
                        ini.key == param && 
                        string.IsNullOrEmpty(value)) return true;

                    else if (ini.picker != null &&
                        ini.picker.SelectedIndex != -1 &&
                        ini.key == param &&
                        string.IsNullOrEmpty(value)) return true;
                }
                return false;
            }
            public bool isErrorMax(string value, string max)
            {
                try
                {
                    bool is_double = double.TryParse(value, out double result);
                    if (is_double) return Convert.ToDouble(value) >= Convert.ToDouble(max);
                    else return value.Length >= Convert.ToDouble(max);
                }
                catch (Exception)
                {
                    return true;
                }
            }
            public bool isErrorMin(string value, string min)
            {
                try
                {
                    bool is_double = double.TryParse(value, out double result);
                    if (is_double) return Convert.ToDouble(value) <= Convert.ToDouble(min);
                    else return value.Length <= Convert.ToDouble(min);
                }
                catch (Exception)
                {
                    return true;
                }
            }
            public bool isErrorInArray(string value, string array)
            {
                var arr = array.Split(',');
                return arr.Contains(value);
            }
            public bool isErrorNotInArray(string value, string array)
            {
                var arr = array.Split(',');
                return !arr.Contains(value);
            }
            public bool isErrorUrl(string value)
            {
                return (Uri.TryCreate(value, UriKind.Absolute, out Uri uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)) == false;
            }
            public bool isErrorGt(string value, string param)
            {
                return GetErrorGreaterAndLower(value, param, true);
            }
            public bool isErrorGte(string value, string param)
            {
                return GetErrorGreaterAndLower(value, param, true, true);
            }
            public bool isErrorLt(string value, string param)
            {
                return GetErrorGreaterAndLower(value, param, false);
            }
            public bool isErrorLte(string value, string param)
            {
                return GetErrorGreaterAndLower(value, param, false, true);
            }
            private bool GetErrorGreaterAndLower(string value, string param, bool is_greater, bool is_equal = false)
            {
                try
                {
                    foreach (var ini in fields)
                    {
                        if (param == ini.key)
                        {
                            string value_param = null;

                            if (ini.entry != null) value_param = ini.entry.Text;
                            else if (ini.editor != null) value_param = ini.editor.Text;
                            else if (ini.picker != null) value_param = ini.list[ini.picker.SelectedIndex];
                            
                            bool is_double_value = double.TryParse(value, out double dob_value);
                            bool is_double_param = double.TryParse(value, out double dob_param);
                            
                            // jika value dari field yang bersangkutan dengan field yang dicari (param) dapat diubah menjadi double
                            if (is_double_param && is_double_value)
                            {

                                if (is_greater && ((is_equal && dob_value >= dob_param) || (!is_equal && dob_value > dob_param)) ) return false;
                                else if (!is_greater && ((is_equal && dob_value <= dob_param) || (!is_equal && dob_value < dob_param)) ) return false;
                            }
                            // jika berupa string, maka perbandingan akan dibandingkan dengan panjang setiap karaketer dari string tersebut
                            else
                            {
                                if (is_greater && ((is_equal && value.Length >= value_param.Length) || (!is_equal && value.Length > value_param.Length))) return false;
                                else if (!is_greater && ((is_equal && value.Length <= value_param.Length) || (!is_equal && value.Length < value_param.Length))) return false;
                            }

                            break;
                        }
                    }
                    return true;
                }
                catch (Exception)
                {
                    return true;
                }
            }

            public bool isErrorDifferent(string value, string param) { return value == param; }
            public bool isErrorEndsWith(string value, string param) 
            {
                var split = param.Split(',');
                foreach(var ini in split)
                {
                    if (value.EndsWith(ini)) return false;
                }
                return true;
            }
            public bool isErrorStartsWith(string value, string param)
            {
                var split = param.Split(',');
                foreach (var ini in split)
                {
                    if (value.StartsWith(ini)) return false;
                }
                return true;
            }
            public bool isErrorBoolean(string value) { return value != "false" && value != "true"; }
            public bool isErrorRegex(string  value, string regex)
            {
                Regex rg = new Regex(@regex);
                return !rg.IsMatch(value);
            }
            public bool isErrorIp (string value)
            {
                return !IPAddress.TryParse(value, out _);
            }
            public bool isErrorIpv4(string value)
            {
                if(IPAddress.TryParse(value, out IPAddress ip))
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) return false;
                }
                return true;
            }
            public bool isErrorIpv6(string value)
            {
                if (IPAddress.TryParse(value, out IPAddress ip))
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6) return false;
                }
                return true;
            }
            public bool isErrorJson(string value)
            {
                try
                {
                    JToken.Parse(value);
                    return false;
                }
                catch(Exception)
                {
                    return true;
                }
            }
            public bool isErrorNotRegex(string value, string regex)
            {
                Regex rg = new Regex(@regex);
                return rg.IsMatch(value);
            }
            public bool isErrorMaxLength(string value, string max) { return value.Length > Convert.ToInt32(max); }
            public bool isErrorMinLength(string value, string min) 
            {  
                try
                {
                    return value.Length < Convert.ToInt32(min);
                }
                catch(Exception)
                {
                    return true;
                }
            }
            public bool isErrorAlphaNumeric(string strToCheck)
            {
                Regex rg = new Regex(@"^[a-zA-Z0-9]*$");
                return !rg.IsMatch(strToCheck);
            }
            public bool isErrorName(string strToCheck)
            {
                Regex rg = new Regex(@"^[a-zA-Z ,.']*$");
                return !rg.IsMatch(strToCheck);
            }
            public bool isErrorAlpha(string strToCheck)
            {
                Regex rg = new Regex(@"^[a-zA-Z]*$");
                return !rg.IsMatch(strToCheck);
            }
            public bool isErrorAlphaDash(string strToCheck)
            {
                Regex rg = new Regex(@"^[a-zA-Z-_]*$");
                return !rg.IsMatch(strToCheck);
            }
            public bool isErrorAlphaNumericDash(string strToCheck)
            {
                Regex rg = new Regex(@"^[a-zA-Z0-9-_]*$");
                return !rg.IsMatch(strToCheck);
            }
            public bool isErrorNumeric(string value)
            {
                try
                {
                    Convert.ToDouble(value);
                    return false;
                }
                catch (Exception)
                {
                    return true;
                }
            }
            public bool isErrorSame(string value, string param)
            {
                foreach (var ini in fields)
                {
                    if (ini.key == param && ini.entry != null) return ini.entry.Text != value;
                    else if (ini.key == param && ini.editor != null) return ini.editor.Text != value;
                }
                return false;
            }
            public bool isErrorInteger(string value)
            {
                try
                {
                    int.Parse(value);
                    if (value.StartsWith("0")) return true;
                    return false;
                }
                catch (Exception)
                {
                    return true;
                }
            }
        }
    }
}