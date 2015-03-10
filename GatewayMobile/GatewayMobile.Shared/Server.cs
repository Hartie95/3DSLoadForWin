using System;
using System.Collections.Generic;
using System.Text;

using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using System.Linq;

using Windows.Storage;

/* this File is based on the httpServer Sample for Windows phone from Nokia */

namespace GatewayMobile
{
    class Server
    {
        private int mPort;
        private StreamSocketListener mListener;
        private bool mIsActive = false;

        private StorageFolder httpFolder = null;
         public bool IsActive
        {
            get { return mIsActive; }
        }

         public Server(int port)
        {
            mPort = port;
        }

        public int changePort(int port)
        {
            bool active = this.mIsActive;
            if (active)
                this.Stop();
            if(port>0)
                this.mPort = port;
            if(active)
            {
                this.Start();
            }
            return this.mPort;
        }

        public void setFolder(StorageFolder folder)
        {
            this.httpFolder = folder;
        }

        public async void Start()
        {
            if (!mIsActive)
            {
                mIsActive = true;
                mListener = new StreamSocketListener();
                mListener.Control.QualityOfService = SocketQualityOfService.Normal;
                mListener.ConnectionReceived += mListener_ConnectionReceived;
                try
                {
                    /*
                     * Todo
                     * add output to catch
                     * */
                    await mListener.BindServiceNameAsync(mPort.ToString());
                }
                catch(Exception ex)
                {

                }
            }

        }

        public void Stop()
        {
            if (mIsActive)
            {
                mListener.Dispose();
                mIsActive = false;
            }
        }

        async void mListener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            await Task.Run(() =>
            {
                HandleRequest(args.Socket);
            });
        }

        private async void HandleRequest(StreamSocket socket)
        {
            //Initialize IO classes
            DataReader reader = new DataReader(socket.InputStream);
            DataWriter writer = new DataWriter(socket.OutputStream);
            writer.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;

            //handle actual HTTP request
            String request = await StreamReadLine(reader);
            string[] tokens = request.Split(' ');

            if (tokens.Length != 3)
            {
                throw new Exception("invalid http request line");
            }

            //string httpMethod = tokens[0].ToUpper();
            string httpUrl = tokens[1];

            //read HTTP headers - contents ignored in this sample
            while (!String.IsNullOrEmpty(await StreamReadLine(reader))) ;
            
            Windows.Storage.StorageFile siteFile = null;
            
            StringBuilder ret = new StringBuilder();
            String outputFile = "";
            if (!httpUrl.Substring(1).Contains("/")&&httpUrl != "/")
            {
               
                String Get = "";  
                String requestedFile="";

                String[] splitted = httpUrl.Substring(1).Split('?');
                if(splitted.Length>0)
                    requestedFile = splitted[0];

                //Get GET parameters(maybe later needed)
                if(splitted.Length>1)
                    Get = splitted[1];

                //check if the requested file is a short version
                switch (requestedFile)
                {
                    case "gw":
                        outputFile = "gw.html";
                        break;
                    case "multi":
                        outputFile = "multi.html";
                        break;
                    default:
                        outputFile = requestedFile;
                        break;
                }

                //load files from http folder, if set
                if(this.httpFolder!=null)
                {
                    try
                    {
                        siteFile = await this.httpFolder.GetFileAsync(outputFile);
                    }
                    catch (Exception e)
                    {
                        siteFile = null;
                    }
                }
                //load files from resources, if the file was not found inside of the http folder;
                if (siteFile == null)
                {
                    try
                    {
                        //check which folder should be used 
                        String Folder = "Sites";
                        if (outputFile.EndsWith(".dat") || outputFile.EndsWith(".bin"))
                        {
                            Folder = "Files";
                        }
                        siteFile = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Resources/" + Folder + "/" + outputFile));
                    }
                    catch(Exception e)
                    {
                        siteFile = null;
                    }
                }
            }

            if (siteFile != null)
            {
                try
                {
                    IBuffer FileBuff;

                    //set Mime Type
                    string mime = "text/plain";
                    if (outputFile.EndsWith(".html") || outputFile.EndsWith(".htm"))
                    {
                        mime = "text/html";
                    }
                    else if (outputFile.EndsWith(".mp4"))
                    {
                        mime = "video/mp4";
                    }

                    //read File to buffer(otherwise the contend will not have the right binary output) 
                    FileBuff = await Windows.Storage.FileIO.ReadBufferAsync(siteFile);


                    //Create the HTTP answer
                    ret.AppendLine("HTTP/1.1 200 OK");
                    ret.AppendLine("Content-Type: "+mime);
                    ret.AppendLine("Accept-Ranges: bytes");
                    ret.AppendLine("");
                    
                    writer.WriteString(ret.ToString());
                    writer.WriteBuffer(FileBuff);
                }
                catch (Exception ex)//any exception leads to an Internal server error
                {
                    writer.WriteString("HTTP/1.0 500 Internal server error\r\n");
                    writer.WriteString("Connection: close\r\n");
                    writer.WriteString("\r\n");
                    writer.WriteString(ex.Message);
                }
            }
            else 
            {
                //output 404, if the file was not found
                writer.WriteString("HTTP/1.0 404 File not found error\r\n");
                writer.WriteString("Connection: close\r\n");
                writer.WriteString("\r\n");
                writer.WriteString("File not found");
               
            } 
            await writer.StoreAsync();//write data actually to the network interface
            socket.Dispose();
        }

        #region static Helper methods
        public static string[] FindIPAddress()
        {
            List<string> ipAddresses = new List<string>();
            var hostnames = NetworkInformation.GetHostNames();
            foreach (var hn in hostnames)
            {
                //IanaInterfaceType == 71 => Wifi
                //IanaInterfaceType == 6 => Ethernet (Emulator)
                if (hn.IPInformation != null && 
                    (hn.IPInformation.NetworkAdapter.IanaInterfaceType == 71 
                    || hn.IPInformation.NetworkAdapter.IanaInterfaceType == 6))
                {
                    string ipAddress = hn.DisplayName;
                    ipAddresses.Add(ipAddress);
                }
            }

            if (ipAddresses.Count < 1)
            {
                return null;
            }
            else           
            {
                return ipAddresses.ToArray();
            }
        }

        private static async Task<string> StreamReadLine(DataReader reader)
        {
            int next_char;
            string data = "";
            while (true)
            {
                await reader.LoadAsync(1);
                next_char = reader.ReadByte();
                if (next_char == '\n') { break; }
                if (next_char == '\r') { continue; }
                data += Convert.ToChar(next_char);
            }
            return data;
        }

        #endregion static Helper methods
    }
}
