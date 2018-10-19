﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.IO;
using System.IO.Ports;

namespace ObtencionDatos
{
    public partial class Form1 : Form
    {
        public static ArrayList listaAgujeros = new ArrayList();
        
        // Creamos una clase agujero, que en principio usamos como estructura
        public class Agujero
        {
            public float x, y;
            public String xy = "X+000000Y+000000";           
            public Mecha mecha;
            public String xStr, yStr;
        }

        // Creamos una clase mecha, que en principio usamos como estructura
        public class Mecha
        {
            public String nombre;
            public float diametro;
            public String diametroStr;
        }

        // Creamos una clase offset, que contiene las correcciones en xy y angulo
        public class Offset
        {
            public float x, y;
            public float angulo;
        }
        
        string readBuffer;
        string pathArchivo;
        int state;
        int numEsquina = 0;
        bool primerAjuste = true;
        int indexAuxiliar = 0;

        int indexAgujero = 0;
        // - Se usa para el método Recibir(), para ir recorriendo toda la listaAgujeros e ir enviandolos para perforación

        int indexMecha = -1;
        // - Se usa para el método Recibir(), para ir posicionando la mecha según se vaya necesitando

        bool archivoAbierto;
        //Offset offsetGeneral;
        Offset offsetReal = new Offset();

        Agujero extremo = new Agujero();
        Agujero agujeroAux = new Agujero();

        Agujero punto1 = new Agujero();
        Agujero punto2 = new Agujero();
        Agujero punto1real = new Agujero();
        Agujero punto2real = new Agujero();

        public String[] esquinas = new String[4];

        public ArrayList listaMechas = new ArrayList();

        //public void Convertir_string_a_xy(Agujero agujero){}

        public Form1()
        {
            InitializeComponent();
            extremo.x = 500;
            extremo.y = 500;
            MmCorreccion.Text = "00.0";
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        public string Convertir_xy_int_a_string(float x, float y)
        {
            string xy = "";
            int x_nuevo, y_nuevo;
            x_nuevo = (int)x * 10;
            y_nuevo = (int)y * 10;

            xy += "X";
            if (x_nuevo >= 0)
            {
                xy += "+";
            }
            else
            {
                xy += "-";
                x_nuevo *= -1;
            }
            xy += x_nuevo.ToString().PadLeft(6, '0');

            xy += "Y";
            if (y_nuevo >= 0)
            {
                xy += "+";
            }
            else
            {
                xy += "-";
                y_nuevo *= -1;
            }
            xy += y_nuevo.ToString().PadLeft(6, '0');

            return xy;
        }

        public void PuntosExtremos()//OK
        {
            Mecha mechaAux1 = (Mecha)listaMechas[0];
            Mecha mechaAux2 = (Mecha)listaMechas[1];
            foreach (Agujero i in listaAgujeros)
            {
                if (i.mecha.nombre == mechaAux1.nombre)
                {
                    if (punto1.x > i.x)
                    {
                        punto1 = i;
                    }
                    else if (punto1.x == i.x && punto1.y < i.y)
                    {
                        punto1 = i;
                    }
                }
                if (i.mecha.nombre == mechaAux1.nombre || i.mecha.nombre == mechaAux2.nombre)
                {
                    if (punto2.y >= i.y)
                    {
                        punto2 = i;
                    }
                    else if (punto2.x == i.x && punto2.y < i.y)
                    {
                        punto2 = i;
                    }
                }
            }
            
            TextBox1.Text += "Puntos para calibración elegidos" + Environment.NewLine;
        }

        // Abrir archivo para leer
        private void AbrirToolStripMenuItem_Click(object sender, EventArgs e)//OK
        {
            OpenFileDialog1.ShowDialog();
            pathArchivo = OpenFileDialog1.FileName;
            if (pathArchivo != "openFileDialog1")
            {
                label1.Text = pathArchivo;
                Leer_Archivo(pathArchivo);
            }
            else
            {
                MessageBox.Show("No se ha seleccionado un archivo", "Error de seleccion");
            }
        }

        // correccion a 0,0 para todos los puntos
        public void CorreccionPuntos()
        {
            Agujero agujeroAux = (Agujero) listaAgujeros[0];
            float minX = agujeroAux.x;
            float minY = agujeroAux.y;

            foreach (Agujero i in listaAgujeros)
            {
                if (i.x < minX)
                {
                    minX = i.x;
                }
                if(i.y < minY)
                {
                    minY = i.y;
                }                
            }
            foreach (Agujero i in listaAgujeros)
            {
                i.x -= minX;
                i.y -= minY;
                i.xy = Convertir_xy_int_a_string(i.x, i.y);
            }
        }

        // Metodo de lectura del archivo en cuestion
        public void Leer_Archivo(string path)//OK
        {
            string textoArchivo = null;
            string[] lineasArchivo = null;
            
            int cantMechas = 0;
            int separador = 0;
            bool igualPorciento = false;
           
            TextBox1.Text += pathArchivo + Environment.NewLine;         // Muestra el path en el TextBox

            try
            {
                System.IO.File.OpenRead(path);
                textoArchivo = System.IO.File.ReadAllText(path);
                lineasArchivo = System.IO.File.ReadAllLines(path);
                archivoAbierto = true;
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine("No se pudo abrir el archivo");
                return;
            }
            catch (ArgumentException e)
            {
                return; // No se seleccionó un archivo
            }
            
            TextBox1.Text += Environment.NewLine;

            for (int i = 0; i < lineasArchivo.Length; i++)       // Cuenta cantidad de mechas
            {
                if (lineasArchivo[i] == "%")
                {
                    igualPorciento = true;
                    separador = i;
                }
                if (igualPorciento && lineasArchivo[i].Contains("T"))
                {
                    TextBox1.Text += lineasArchivo[i];
                    cantMechas++;
                }
            }

            TextBox1.Text += Environment.NewLine + "Mechas:" + cantMechas + Environment.NewLine;

            for (int i = 0; i < cantMechas; i++)        // Escribo la lista de mechas 
            {
                if (lineasArchivo[i + 2].Contains("T")) // i+2 ???
                {
                    Mecha mecha = new Mecha
                    {
                        nombre = lineasArchivo[i + 2].Substring(0, 3),
                        diametro = float.Parse(lineasArchivo[i + 2].Substring(4, 6))/1000,   //En este VS hay que dividir por 1000, en el VS 2017 no
                        diametroStr = lineasArchivo[i + 2].Substring(4, 6)
                    };
                    listaMechas.Add(mecha);
                }
            }
            
            //String mechaActual;
            Mecha auxMecha = new Mecha();
            //float diametroMechaActual = 0;

            for (int i = separador; i < lineasArchivo.Length; i++)      // Recorre el archivo
            {
                if (lineasArchivo[i].Substring(0, 1) == "T")            // Busca la mecha actual
                {
                    auxMecha.nombre = lineasArchivo[i].Substring(0, 3);
                    foreach (Mecha mecha in listaMechas)
                    {
                        if (mecha.nombre == auxMecha.nombre)
                        {
                            auxMecha.diametro = mecha.diametro;       // Le asigna el diametro a la mecha actual
                        }
                    }
                }
                if (lineasArchivo[i].Substring(0, 1) == "X")            // Busca agujeros a realizar por esa mecha
                {
                    Agujero agujero = new Agujero
                    {
                        //x = Int32.Parse(lineasArchivo[i].Substring(1, lineasArchivo[i].IndexOf('Y', 1) - 1)),
                        //y = Int32.Parse(lineasArchivo[i].Substring(lineasArchivo[i].IndexOf('Y', 1) + 1, lineasArchivo[i].Length - (lineasArchivo[i].IndexOf('Y', 1) + 1)))
                        xStr = lineasArchivo[i].Substring(1, lineasArchivo[i].IndexOf('Y', 1) - 1),
                        yStr = lineasArchivo[i].Substring(lineasArchivo[i].IndexOf('Y', 1) + 1, lineasArchivo[i].Length - (lineasArchivo[i].IndexOf('Y', 1) + 1))
                    };
                    agujero.mecha = new Mecha
                    {
                        diametro = auxMecha.diametro,
                        nombre = auxMecha.nombre
                    };
                    if (agujero.xStr.Length -1 < 6)
                    {
                        string ceros = null;
                        int largoCoord = agujero.xStr.Length -1;
                        for (int j = 0; j < 6 - largoCoord; j++)
                        {
                            ceros += '0';
                        }
                        agujero.xStr = agujero.xStr.Substring(0,1) + ceros + agujero.xStr.Substring(1,agujero.xStr.Length-1);
                    }
                    if (agujero.yStr.Length -1 < 6)
                    {
                        string ceros = null;
                        int largoCoord = agujero.yStr.Length-1;
                        for (int j = 0; j < 6 - largoCoord; j++)
                        {
                            ceros += '0';
                        }
                        agujero.yStr = agujero.yStr.Substring(0,1)+ ceros + agujero.yStr.Substring(1,agujero.yStr.Length-1);
                    }
                    agujero.xy = "X" + agujero.xStr + "Y" + agujero.yStr;
                    //agujero.xy = Convertir_xy_int_a_string(agujero.x, agujero.y);
                    listaAgujeros.Add(agujero);
                }
            }
            //CorreccionPuntos();
            TextBox1.Text += "Listas Terminadas" + Environment.NewLine;
            //PuntosExtremos();
            TextBox1.Text += "Puntos para calibración guardados" + Environment.NewLine;
        }

        // Metodo para resetear agujeros a 0
        public void ResetAgujero(Agujero agujero)//OK
        {
            agujeroAux.x = 0;
            agujeroAux.y = 0;
            agujeroAux.xy = "X+000000Y+000000";
        }

        public void EnableButtons(bool estado)//OK
        {
            CorreccionXmas.Enabled = estado;
            CorreccionYmas.Enabled = estado;
            CorreccionXmenos.Enabled = estado;
            CorreccionYmenos.Enabled = estado;
        }

        public void EnviarMecha(Mecha mechaAEnviar)
        {
            string mecha;
            
            mecha = "M";
            //mecha += mechaAEnviar.diametro.ToString();
            mecha += mechaAEnviar.diametroStr;
            mecha = mecha.Replace(",", ".");
            while (mecha.Length < 6)
            {
                mecha += "0";
            }
            Enviar(mecha);
        }


        public void Leer_Archivo_Esquinas(String path)
        {

            string textoArchivo = null;
            string[] lineasArchivo = null;

            //Propias
            int indexArchivo = 0;
            string esquinaAux=null;
            int contadorNumero;
            int j;
            string coordAux = null;
            string esquinaFinalAux = null;
            int inicioCoordY;
            int largoCoord;
            try
            {
                System.IO.File.OpenRead(path);
                textoArchivo = System.IO.File.ReadAllText(path);
                lineasArchivo = System.IO.File.ReadAllLines(path);
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine("No se pudo abrir el archivo");
                return;
            }
            catch (ArgumentException e)
            {
                return; // No se seleccionó un archivo
            }


            //Posicionamiento del indice de lectura de las esquinas
            while (lineasArchivo[indexArchivo][0] != 'X')
                indexArchivo++;

            for (int i = 0; i < 4; i++)
            {
                esquinaAux = lineasArchivo[indexArchivo+i];
                contadorNumero = 0;
                j = 1;
                while (esquinaAux[++j] != 'Y')
                    contadorNumero++;
                j++;
                inicioCoordY = j+1;
                coordAux = esquinaAux.Substring(2, contadorNumero);
                if (contadorNumero > 1)
                {
                    coordAux = coordAux.Substring(0, coordAux.Length - 2); // le saco los dos ceros del final
                    if (contadorNumero - 2 <= 5)
                    {
                        string ceros = null;
                        contadorNumero -= 2;
                        for (int k = 0; k < 6 - contadorNumero; k++)
                        {
                            ceros += '0'; 
                        }
                        coordAux = ceros + coordAux; // Le agrego ceros a la izquierda
                    }
                }

                largoCoord = coordAux.Length;
                for (int k = 0; k < 6 - largoCoord; k++) // Rellena con ceros a la derecha si hace falta (para tener 6 digitos)
                    coordAux += '0';
                
                esquinaFinalAux = "X+" + coordAux;

                contadorNumero = 0;
                while (esquinaAux[++j] != 'D')
                    contadorNumero++;
                coordAux = esquinaAux.Substring(inicioCoordY, contadorNumero);
                if (contadorNumero > 1)
                {
                    coordAux = coordAux.Substring(0, coordAux.Length - 2); // le saco los dos ceros del final
                    if (contadorNumero - 2 <= 5)
                    {
                        string ceros = null;
                        contadorNumero -= 2;
                        for (int k = 0; k < 6 - contadorNumero; k++)
                        {
                            ceros += '0';
                        }
                        coordAux = ceros + coordAux; // Le agrego ceros a la izquierda (para tener 6 digitos)
                    }
                }
                largoCoord = coordAux.Length;
                for (int k = 0; k < 6 - largoCoord; k++) // Rellena con ceros a la derecha si hace falta (para tener 6 digitos)
                    coordAux += '0';
                esquinaFinalAux += "Y+" + coordAux;

                //Para aca ya se tiene la esquina cargada en esquinaFinalAux
                //Carga en el vector de esquinas
                esquinas[i] = esquinaFinalAux;
            }

        }


        // Metodo para secuencia completa de calibracion
        public void Calibracion()
        {
            Offset correccion = new Offset();   
            Offset correccionPunto1 = new Offset();
            Offset correccionPunto2 = new Offset();
            Agujero punto1 = new Agujero();
            Agujero punto2 = new Agujero();
            Agujero punto2real = new Agujero();
            //enableButtons(true);
            MmCorreccion.Enabled = true;
            Calibrar.Enabled = false;

            // Calibración eje z
            //EnviarAgujero(extremo); // Envia agujero de extremo para prueba de profundidad
            /*
            while (readBuffer != "M")      // Espera hasta recibir confirmacion de fin de secuencia
            {
                Recibir();
            }            

            
            mechaEnviar = (Mecha)listaMechas[0];    
            EnviarMecha(mechaEnviar.diametro);

            EnableButtons(true);
            */
            /*
            while (readBuffer != "A")      // Espera hasta recibir inicio de ajuste
            {
                Recibir();                
            }
            readBuffer = "";
            Enviar("A");    // Primer Agujero ajuste
            // Calibación plano xy
            Enviar(punto1.xy); // Envia primer punto de referencia
            agujeroAux = punto1;    // Guarda punto en variable auxiliar
            
            TextBox1.Text += "Punto 1 Recibido" + Environment.NewLine;
            */
            /*
            while (readBuffer != "A")
            {                
                Recibir();                
            }
            readBuffer = "";
            Enviar("A");    //Srgundo Agujero ajuste
            Enviar(punto2.xy); // Envia primer punto de referencia
            CalibracionLista.Enabled  = true;
            
            TextBox1.Text += "Punto 2 Recibido" + Environment.NewLine;
             */
            /*
            while (readBuffer != "F")//Extender a todo el metodo para hacerlo varias veces
            {
                Recibir();
            }
            */
            return;
        }
        
        // Envio de datos
        public void Enviar(string caracteres)//OK
        {
            if (PuertoSerie.IsOpen)
            {
                PuertoSerie.Write(caracteres);
            }
            else
            {
                MessageBox.Show("Abrir el puerto para mandar el dato \" " + caracteres + "\"");
            }
            
        }

        // Actualizacion del textbox de recepcion
        public void ActualizarTexto(object sender, EventArgs e)//OK
        {
            TxtRecibir.Text += readBuffer;
        }

        
        // Guarda caracteres en readBuffer
        public void Recibir()// NO OK - TERMINAR!!!!!!!!!!!!!!!uno
        {
            String estado = "";
            Agujero agujeroAuxiliar = new Agujero();
            
            
            /*if(PuertoSerie.IsOpen) // Maneja si no está abierto
            {
                try
                {
                    estado = PuertoSerie.ReadExisting();
                    //this.Invoke(new EventHandler());
                }
                catch(Exception e)
                {
                    MessageBox.Show("Error" + e.Message);
                }
            }*/
            estado = PuertoSerie.ReadExisting();
            switch (estado[0])
            {
                case 'M':  // - Lo primero que envía el micro
                    indexMecha ++;
                    EnviarMecha((Mecha)listaMechas[indexMecha]); // - Ya envia correctamente
                    //EnableButtons(true); ver
                    
                    //VER CUANDO SE INICIALIZA EL AGUJEROAUXILIAR -------------
                    /*while (agujeroAuxiliar.mecha == (Mecha)listaMechas[indexMecha]) // - Posiciona al indexAuxiliar en el agujero que tenga la próxima mecha
                    {
                        agujeroAuxiliar = (Agujero) listaAgujeros[indexAuxiliar]; // - agujeroAuxiliar queda en la próxima mecha
                        indexAuxiliar++; // - indexAuxiliar queda en la posición de la próxima mecha
                    }*/
                    break;
                case 'A':
                    if (primerAjuste)
                    {
                        primerAjuste = false;
                        Enviar("A");
                    }
                    Enviar(esquinas[numEsquina++]);
                    if (numEsquina == 4)
                        numEsquina = 0;
                    break;
                    /*
                     * case 'A': // - Lo segundo que envía el micro
                    if (primerAjuste)
                    {
                        primerAjuste = false;
                        Enviar("A");    // Primer Agujero ajuste
                        // Calibación plano xy
                        
                        Enviar(punto1.xy); // Envia primer punto de referencia
                        agujeroAux = punto1;    // Guarda punto en variable auxiliar

                        //TextBox1.Text += "Punto 1 Recibido" + Environment.NewLine;
                    }
                    else
                    {
                        //Enviar("A");    //Segundo Agujero ajuste // - NO TIENE QUE VOLVER A MANDAR UNA 'A'
                        Enviar(punto2.xy); // Envia segundo punto de referencia
                        //CalibracionLista.Enabled = true;
                        //TextBox1.Text += "Punto 2 Recibido" + Environment.NewLine;
                    }
                    break; 
                *
                */
                case 'F': // - Lo tercero que envía el micro
                    //TextBox1.Text += "Ajuste terminado" + Environment.NewLine;
                    Enviar("*"); // - Agregado, da orden de inicio de perforación
                    break;
                case 'P': // - Orden de perforación
                    Enviar("P"); // - El primer agujero de perforación se envía como "PX+xxxxxxY+xxxxxxx"
                    /*
                    do
                    {
                        agujeroAuxiliar = (Agujero) listaAgujeros[indexAgujero]; // - Primer uso de indexAgujero
                        indexAgujero++;
                    } while (agujeroAuxiliar.mecha == listaMechas[indexMecha]); */
                    Enviar(((Agujero)listaAgujeros[indexAgujero]).xy);
                    indexAgujero++;
                    break;
                case '*':
                    Enviar(((Agujero)listaAgujeros[indexAgujero]).xy); // SE VA DE RANGO
                    if (listaAgujeros.Count > indexAgujero + 1)
                    {
                        if (((Agujero)listaAgujeros[indexAgujero]).mecha == ((Agujero)listaAgujeros[indexAgujero + 1]).mecha)
                            Enviar("P");
                    }
                    else
                    {
                        Enviar("F");
                    }
                    /*if (indexAgujero == indexAuxiliar)
                    {
                        Enviar("P"); // - Agregar una P al final del último punto de la misma mecha
                    }*/
                    indexAgujero++;
                    break;
                case 'O':
                    Enviar("*");
                    primerAjuste = true;
                    break;
            }
            
        }

        public void CalculosOffset(Agujero punto1, Agujero punto2, Agujero punto2real)
        {
            float alpha, beta, gamma;
            Agujero puntoACorregir = new Agujero();
            Agujero puntoAux = new Agujero();

            alpha = (float) Math.Atan((punto1.y - punto2.y) / (punto1.x - punto2.x));
            beta = (float) Math.Atan((punto1.y - punto2real.y) / (punto1.x - punto2real.x));
            gamma = beta - alpha;

            offsetReal.angulo = gamma;

            /***Esto hay que hacerlo en otro metodo para todos los puntos a corregir***/
            puntoAux.x = puntoACorregir.x - punto1.x;
            puntoAux.y = puntoACorregir.y - punto1.y;

            puntoAux.x = (float) (puntoAux.x * Math.Cos(gamma) - puntoAux.y * Math.Sin(gamma));
            puntoAux.y = (float) (puntoAux.y * Math.Sin(gamma) + puntoAux.y * Math.Cos(gamma));

            puntoAux.x = puntoACorregir.x + punto1.x;
            puntoAux.y = puntoACorregir.y + punto1.y;
            /**********************************************************/

        }
                
        // Envia posicion de agujero
        public void EnviarAgujero(Agujero agujero)
        {
            Enviar(Convertir_xy_int_a_string(agujero.x, agujero.y));    // Envia posicion de cambio de mecha para bajar y hacer prueba de altura 
            /*while (readBuffer != "*")      // Espera hasta recibir confirmacion de la posicion
            {
                Recibir();
            }*/
            //resetReadBuffer();
        }

        private void Calibrar_Click(object sender, EventArgs e)//OK
        {
            Enviar("S");
            Calibracion();
        }

        private void ToolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)//OK
        {
            CboPuertoSerie.Items.Clear();

            if (!PuertoSerie.IsOpen)
            {
                try
                {
                    CboPuertoSerie.Items.AddRange(SerialPort.GetPortNames());
                }
                catch { }
            }
            else
            {
                // Si el puerto esta abierto, no muestra la lista
            }
        }              
        
        private void PuertoSerie_DataReceived_1(object sender, SerialDataReceivedEventArgs e)
        {
            /*if (PuertoSerie.IsOpen)
            {
                try
                {
                    readBuffer = PuertoSerie.ReadExisting();
                    this.Invoke(new EventHandler(ActualizarTexto));
                }
                catch (Exception ex)
                {
                }
            }*/
            Recibir();
        }

        private void Ciclo_Agujereado()
        {
            /*
            int i = 1;
            Mecha mechaAnterior = (Mecha) listaMechas[0];
            Mecha mechaActual = (Mecha) listaMechas[0];
            float diamMechaAnt = mechaAnterior.diametro;
            float diamMechaAct = mechaAnterior.diametro;
            */
            /*Enviar("S");
            foreach (Agujero agujero in listaAgujeros)
            {
                diamMechaAct = agujero.mecha.diametro;
                if (diamMechaAct != diamMechaAnt)
                {
                    TextBox1.Text += "Inicio cambio mecha" + Environment.NewLine;
                    Enviar("C");
                    while (readBuffer != "M")   // Confirmacion cambio de mecha
                    {
                        Recibir();
                    }
                    EnviarMecha(diamMechaAct);
                    while (readBuffer != "C")   // Confirmacion cambio de mecha
                    {
                        Recibir();                        
                    }
                    readBuffer = "";
                    TextBox1.Text += "Inicio cambio mecha listo" + Environment.NewLine;
                    diamMechaAnt = diamMechaAct;
                }
                Enviar("P");    // Envia punto
                EnviarAgujero(agujero);
                while (readBuffer != "*")   // Confirmacion de perforacion lista
                {
                    Recibir();
                }
                readBuffer = "";
                TextBox1.Text += "Agujero Listo" + i + Environment.NewLine;
                if (i == listaAgujeros.Capacity)    // Si es el ultimo agujero envia F
                {
                    Enviar("F");
                    TextBox1.Text += "Fin Perforacion" + i + Environment.NewLine;
                }
                i++;                
            }*/
        }

        private void BtnEnviar_Click(object sender, EventArgs e)//OK
        {
            Enviar(TxtEscribir.Text);
            TxtEscribir.Text = "";
        }
        private void BtnAbrirCerrar_Click_1(object sender, EventArgs e)//OK
        {
            if (PuertoSerie.IsOpen)
            {
                PuertoSerie.DiscardInBuffer();
                PuertoSerie.Close();
                BtnAbrirCerrar.Text = "Abrir Puerto";
                Calibrar.Enabled = false;
            }
            else
            {
                try
                {
                    PuertoSerie.PortName = CboPuertoSerie.SelectedItem.ToString();
                    PuertoSerie.Open();
                    Calibrar.Enabled = true;
                    BtnAbrirCerrar.Text = "Cerrar Puerto";
                }
                catch (UnauthorizedAccessException ex)
                {
                    MessageBox.Show("El puerto " + CboPuertoSerie.SelectedItem + " está ocupado.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("El puerto " + CboPuertoSerie.SelectedItem + " no se ha podido abrir satisfactoriamente.");
                }
            }
        }
        private void CalibracionLista_Click(object sender, EventArgs e)
        {
            switch (state)
            {
                case 0:
                    offsetReal.x = agujeroAux.x + punto1.x; //Revisar
                    offsetReal.y = agujeroAux.y + punto1.y; //Revisar
                    punto1 = agujeroAux;    //Guarda variable auxiliar con corrección en punto de referencia
                    punto2.x += offsetReal.x;
                    punto2.y += offsetReal.y;

                    CalibracionLista.Text = "Finalizar";

                    Enviar(punto2.xy); //Envia segundo punto de referencia
                    agujeroAux = punto2; // Guarda punto en variable auxiliar

                    while (readBuffer != "*")
                    {
                        Recibir();
                    }
                    TextBox1.Text += "Agujero recibido" + Environment.NewLine;
                    state = 1;

                    break;
                case 1:
                    CalculosOffset(punto1, punto2, punto2real);
                    punto2 = agujeroAux; // Guarda variable auxiliar con corrección en punto de referencia

                    //EnableButtons(false); ver
                    MmCorreccion.Enabled = false;
                    Calibrar.Enabled = true;
                    state = 0;
                    CalibracionLista.Enabled = false;

                    break;
            }
        }
        private void CorreccionYmas_Click(object sender, EventArgs e)//OK Sujeto a cambios
        {
            agujeroAux.y += float.Parse(MmCorreccion.Text, System.Globalization.CultureInfo.InvariantCulture);
            EnviarAgujero(agujeroAux);
        }
        private void CorreccionXmas_Click(object sender, EventArgs e)//OK Sujeto a cambios
        {
            agujeroAux.x += float.Parse(MmCorreccion.Text, System.Globalization.CultureInfo.InvariantCulture);
            EnviarAgujero(agujeroAux);
        }
        private void CorreccionXmenos_Click(object sender, EventArgs e)//OK Sujeto a cambios
        {
            agujeroAux.y -= float.Parse(MmCorreccion.Text, System.Globalization.CultureInfo.InvariantCulture);
            EnviarAgujero(agujeroAux);
        }
        private void CorreccionYmenos_Click(object sender, EventArgs e)//OK Sujeto a cambios
        {
            agujeroAux.x -= float.Parse(MmCorreccion.Text, System.Globalization.CultureInfo.CurrentCulture);
            EnviarAgujero(agujeroAux);
        }
        private void BtnComenzar_Click(object sender, EventArgs e)//OK
        {
            Enviar("*");
            //Ciclo_Agujereado();
            //EnableButtons(true);
        }
        private void VisualizarPuntos_Click(object sender, EventArgs e)//Revisar
        {
            Form2 form2 = new Form2();
            form2.Show();
        }

        //Leer las esquinas
        private void esquinasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog1.ShowDialog();
            pathArchivo = OpenFileDialog1.FileName;
            if (pathArchivo != "openFileDialog1")
            {
                label1.Text = pathArchivo;
                Leer_Archivo_Esquinas(pathArchivo);
            }
            else
            {
                MessageBox.Show("No se ha seleccionado un archivo", "Error de seleccion");
            }
        }
    }
}

//TODO: VER USAR THREADS
//TODO: Mover punto 1 teorico a real con botones
//TODO: Mover punto 2 teorico a real con botones
//TODO: No corregir a origen si esta dentro de los limites

