using BoomBang.game.manager;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Net.Sockets;
using System.Net;
using BoomBang.server;
using System.Threading;

namespace BoomBang.Forms
{
    public partial class Emulator : Form
    {
        public static string estado = "Premier";//GitHub
        public static string vercion = "v4.0.1";
        public static bool threads_especiales = true;
        public static bool ver_conexion_usuarios = true;
        public static readonly Encoding Encoding = Encoding.GetEncoding("iso-8859-1");
        public static int puerto_server = 2002;
        public static Emulator Form;

        public Emulator()
        {
            this.FormClosing += Emulator_FormClosing;
            InitializeComponent();
        }

        private void Emulator_Load(object sender, EventArgs e)
        {
            UpdateTitle();
            loadMysqlDll();
            Start();
        }
        private void Emulator_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(0);
        }
        private void Start()
        {
            try
            {
                Form = this;
                WriteLine($"Emulador Enterprise Edition Premier {vercion}", "success");
                SessionManager.Initialize(new TcpListener(IPAddress.Any, puerto_server));
                HandlerManager.Initialize();
                SalasManager.Initialize();
                WriteLine("Visualizar la conexión de usuarios: " + (ver_conexion_usuarios == true ? "true" : "false"));
                WriteLine("Servidor iniciado correctamente!", "success");
                WriteLine("____________________________________________________________");
                ServerThreads.Initialize();
                Console.Beep();

            }
            catch (Exception ex)
            {
                console.AppendText(Environment.NewLine + ex.ToString());
            }
        }
        public void UpdateTitle()
        {
            Text = $"Emulador Enterprise Edition {estado} | Online: " + UserManager.UsuariosOnline.Count;
        }
        private static void loadMysqlDll()
        {
            string resource1 = "BoomBang.MySql.Data.dll";
            EmbeddedAssembly.Load(resource1, "MySql.Data.dll");
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
        }
        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return EmbeddedAssembly.Get(args.Name);
        }
        public static void EditorialResponse(Exception ex)
        {
            /*string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Errores\RegistroEmulador.txt");
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                writer.WriteLine("-----------------------------------------------------------------------------");
                writer.WriteLine("Date : " + DateTime.Now.ToString());
                writer.WriteLine();

                writer.WriteLine(ex.GetType().FullName);
                writer.WriteLine("Message : " + ex.Message);
             +   writer.WriteLine("StackTrace : " + ex.StackTrace);
                ex = ex.InnerException;
            }*/
        }

        public void WriteLine(string text)
        {
            CheckForIllegalCrossThreadCalls = false;

            console.SelectionColor = Color.White;

            string output = DateTime.Now.ToString("HH:mm:ss") + " -> " + text;
            console.AppendText(Environment.NewLine + output);
        }

        public void WriteLine(string text,  string status)
        {
            CheckForIllegalCrossThreadCalls = false;
            switch (status)
            {
                case "warning":
                    console.SelectionColor = Color.Yellow;
                    break;
                case "success":
                    console.SelectionColor = Color.GreenYellow;
                    break;
                case "error":
                    console.SelectionColor = Color.Red;
                    break;
                case "normal":
                    console.SelectionColor = Color.White;
                    break;
            }
            string output = DateTime.Now.ToString("HH:mm:ss") + " -> " + text;
            console.AppendText(Environment.NewLine + output);
            console.SelectionColor = Color.White;
        }
    }
}
