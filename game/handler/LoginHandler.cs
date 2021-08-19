using BoomBang.game.instances;
using BoomBang.game.manager;
using BoomBang.game.packets;
using BoomBang.server;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BoomBang.Forms;

namespace BoomBang.game.handler
{
    class LoginHandler
    {
        
        public static void Start()
        {
            HandlerManager.RegisterHandler(120145120, Facebook);
            HandlerManager.RegisterHandler(120139, registro_paso_1);
            HandlerManager.RegisterHandler(120131, registro_paso_2);
            HandlerManager.RegisterHandler(148135, reactivar_cuenta);
            HandlerManager.RegisterHandler(120130, iniciar_sesion);
            HandlerManager.RegisterHandler(121123, recuperacion);
            HandlerManager.RegisterHandler(121130, validar_recu);
            HandlerManager.RegisterHandler(121136, cambiar_contra);
        }
        private static void iniciar_sesion(SessionInstance Session, string[,] Parameters)
        {
            new Thread(() => stratAntiScriptSession(Session)).Start();
         
            string userName = Parameters[0, 0];
            string passwordUser = Parameters[1, 0];
            if (userName != "" && passwordUser != "")
            {
                UserManager.IniciarSesion(Session, userName, passwordUser);
            }
            else
            {
                string console = "Error al iniciar cliente " + Session.IP;
                Emulator.Form.WriteLine(console);
            }
        }
        private static void stratAntiScriptSession(SessionInstance Session)
        {
            int timer = 10;
            while (timer > 0)
            {
                if (Session.User != null)
                {
                    timer -= 1;
                }    
                Thread.Sleep(1000);
            }
            Session.User.sendDataUser = 0;
            Session.User.startAntiScript = true;
        }

        private static bool validarMultiKekos(SessionInstance Session)
        {
            int countKekos = 0;
            foreach (UserInstance usuariosRegistrados in UserManager.usuariosRegistrados.Values.ToList())
            {
                if (usuariosRegistrados.ip_registro == Session.IP || usuariosRegistrados.ip_actual == Session.IP)
                {
                    countKekos += 1;
                }
            }
            if (countKekos > 5)
            {
                return false;
            }
            return true;
        }
        static void registro_paso_2(SessionInstance Session, string[,] Parameters)
        {
            string nombre = Parameters[0, 0];
            if (nombre != null)
            {
                if (nombre == "") return;
                if (nombre.Contains("ñ")) return;
                if (nombre.Length > 15) return;
                if (Session.ValidarEntrada(nombre, true))
                {
                    if (string.IsNullOrEmpty(nombre)) { return; }
                    string contraseña = Parameters[1, 0];
                    int avatar = int.Parse(Parameters[2, 0]);
                    if (avatar <= 0 || avatar >= 12) { return; }
                    string colores = Parameters[3, 0];
                    int edad = int.Parse(Parameters[4, 0]);
                    string email = Parameters[5, 0];
                    if (edad > 100) return;
                    UserInstance Usuario = UserManager.ObtenerUsuario(nombre);
                    if (Usuario != null) { return; }
                    //if (validarMultiKekos(Session) == false) { return; }
                    if (UserManager.RegistrarUsuario(nombre, contraseña, avatar, colores, edad, email, Session.IP))
                    {
                        UserManager.IniciarSesion(Session, nombre, contraseña);
                    }
                }
            }
        }
        static void registro_paso_1(SessionInstance Session, string[,] Parameters)//Registro nombre usuario
        {
            if (string.IsNullOrEmpty(Parameters[0, 0])) { return; }
            Packet_120_139(Session, Parameters[0, 0]);
        }
        static void Facebook(SessionInstance Session, string[,] Parameters)
        {

        }
        static void reactivar_cuenta(SessionInstance Session, string[,] Parameters)
        {
            if (Session.User == null) return;
            ServerMessage server = new ServerMessage();
            server.AddHead(148);
            server.AddHead(135);
            try
            {
                using (mysql client = new mysql())
                {
                    client.SetParameter("id", Session.User.id);
                    if (client.ExecuteNonQuery("DELETE FROM cuentas_desactivadas WHERE id = @id") == 1)
                    {
                        server.AppendParameter(1);
                    }
                    else
                    {
                        server.AppendParameter(-1);
                    }
                }

            }
            catch
            {
                server.AppendParameter(-1);
            }
            Session.SendData(server);
        }
        private static void Packet_120_139(SessionInstance Session, string nombre)
        {
            ServerMessage server = new ServerMessage();
            server.AddHead(120);
            server.AddHead(139);
            UserInstance User = UserManager.ObtenerUsuario(nombre);
            if (User != null)
            {
                server.AppendParameter(1);
            }
            else
            {
                server.AppendParameter(2);
            }
            Session.SendDataProtected(server);
        }

        static void cambiar_contra(SessionInstance Session, string[,] Parameters)
        {
            mysql client = new mysql();
            client.SetParameter("codigo", int.Parse(Parameters[0, 0]));
            DataRow recuperacion = client.ExecuteQueryRow("SELECT * FROM recuperaciones WHERE codigo = @codigo");
            ServerMessage server = new ServerMessage();
            server.AddHead(121);
            server.AddHead(136);
            if (recuperacion != null)
            {
                client.SetParameter("id", (int)recuperacion["id_usuario"]);
                DataRow datos_user = client.ExecuteQueryRow("SELECT * FROM usuarios WHERE id = @id");
                if (datos_user != null)
                {
                    string password = Parameters[1, 0];
                    if (password == (string)datos_user["password"])
                    {
                        server.AppendParameter(-1);
                        Session.SendData(server);
                        return;
                    }
                    client.SetParameter("user_id", (int)recuperacion["id_usuario"]);
                    client.SetParameter("pass", password);
                    client.ExecuteNonQuery("UPDATE usuarios SET password = @pass WHERE id = @user_id");
                    server.AppendParameter(1);
                    Session.SendData(server);
                    client.SetParameter("codigo", int.Parse(Parameters[0, 0]));
                    client.ExecuteNonQuery("DELETE FROM recuperaciones WHERE codigo = @codigo");

                    email_manager((string)datos_user["email"], "BurBian Password", "Guarda tu nueva contraseña y no se la digas a nadie : " + Parameters[1, 0]);
                    return;
                }
            }
            server.AppendParameter(-1);
            Session.SendData(server);
        }
        static void validar_recu(SessionInstance Session, string[,] Parameters)
        {
            mysql client = new mysql();
            client.SetParameter("codigo", Parameters[0, 0]);
            DataRow recuperacion = client.ExecuteQueryRow("SELECT * FROM recuperaciones WHERE codigo = @codigo");
            ServerMessage server = new ServerMessage();
            server.AddHead(121);
            server.AddHead(130);
            if (recuperacion != null)
            {
                server.AppendParameter(1);
                Session.SendData(server);
                return;
            }
            server.AppendParameter(0);
            Session.SendData(server);
        }
        static void recuperacion(SessionInstance Session, string[,] Parameters)
        {
            mysql client = new mysql();
            client.SetParameter("nombre", Parameters[0, 0]);
            client.SetParameter("email", Parameters[1, 0]);
            DataRow usuario = client.ExecuteQueryRow("SELECT * FROM usuarios WHERE nombre = @nombre AND email = @email");
            ServerMessage server = new ServerMessage();
            server.AddHead(121);
            server.AddHead(123);
            if (usuario != null)
            {
                client.SetParameter("id", (int)usuario["id"]);
                DataRow recuperacion = client.ExecuteQueryRow("SELECT * FROM recuperaciones WHERE id_usuario = @id");
                if (recuperacion != null)
                {
                    server.AppendParameter(-1);
                    Session.SendData(server);
                    return;
                }
                List<int> codigos = new List<int>();
                foreach (DataRow codigo in client.ExecuteQueryTable("SELECT * FROM recuperaciones").Rows)
                {
                    codigos.Add((int)codigo["codigo"]);
                }
                int codigo_recuperacion = new Random().Next(100000, 999999);
                while (codigos.Contains(codigo_recuperacion))
                {
                    codigo_recuperacion = new Random().Next(100000, 999999);
                }
                client.SetParameter("id_usuario", (int)usuario["id"]);
                client.SetParameter("codigo", codigo_recuperacion);
                client.ExecuteNonQuery("INSERT INTO recuperaciones (id_usuario, codigo) VALUES (@id_usuario, @codigo)");
                server.AppendParameter(1);
                Session.SendData(server);
                new Thread(() => recuperar_password_manager()).Start();
                return;
            }
            server.AppendParameter(-1);
            Session.SendData(server);
        }
        static void recuperar_password_manager()
        {
            mysql client = new mysql();
            double tiempo = Time.GetCurrentAndAdd(AddType.Minutos, 5);
            foreach (DataRow recuperacion in client.ExecuteQueryTable("SELECT * FROM recuperaciones WHERE codigo != '' AND enviado = 0").Rows)
            {
                client.SetParameter("id", recuperacion["id_usuario"]);
                DataRow usuario = client.ExecuteQueryRow("SELECT * FROM usuarios WHERE id = @id");
                ////////////////////////////////ENVIAR EL EMAIL CON CODIGO////////////////////////////////////
                email_manager((string)usuario["email"], "Codigo recuperacion",
                    "Tu codigo de recuperacion es: " + Convert.ToString((int)recuperacion["codigo"]));
                /////////////////////////////////////////////////////////////////////////////////////
                client.SetParameter("id", recuperacion["id_usuario"]);
                client.SetParameter("enviado", tiempo);
                client.ExecuteNonQuery("UPDATE recuperaciones SET enviado = @enviado WHERE id_usuario = @id");
            }
            while (Time.GetDifference(tiempo) > 0)
            {
                Thread.Sleep(new TimeSpan(0, 0, 1));
            }
            foreach (DataRow borar_recuperacion in client.ExecuteQueryTable("SELECT * FROM recuperaciones WHERE enviado > 0").Rows)
            {
                if (Time.GetDifference((int)borar_recuperacion["enviado"]) <= 0)
                {
                    client.SetParameter("id", (int)borar_recuperacion["id_usuario"]);
                    client.ExecuteNonQuery("DELETE FROM recuperaciones WHERE id_usuario = @id");
                }
            }
        }
        static void email_manager(string destinatario, string titulo, string body)
        {
            //////https://www.youtube.com/watch?v=oIg4JF2Xfe0
            System.Net.Mail.SmtpClient mailClient = new System.Net.Mail.SmtpClient("smtp.gmail.com", 587);
            System.Net.Mail.MailMessage MyMailMessage = new System.Net.Mail.MailMessage("email@gmail.com", destinatario,
            titulo, body);
            MyMailMessage.IsBodyHtml = false;
            System.Net.NetworkCredential mailAuthentication = new System.Net.NetworkCredential("email@gmail.com", "Mamadu1239");
            mailClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            mailClient.EnableSsl = true;
            mailClient.UseDefaultCredentials = false;

            mailClient.Credentials = mailAuthentication;
            try
            {
                mailClient.Send(MyMailMessage);
            }
            catch (Exception exc)
            {
            
 
            }
        }
    }
}
