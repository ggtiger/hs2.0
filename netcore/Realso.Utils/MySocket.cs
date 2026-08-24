using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace Realso.Utils
{
  public class MySocket
  {
    private static Encoding encode = Encoding.UTF8;
    public static string Send(string host, int port, string data)
    {
      string result = string.Empty;
      try
      {
        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        clientSocket.Connect(host, port);
        clientSocket.Send(encode.GetBytes(data));
        Console.WriteLine("Send：" + data);
        result = Receive(clientSocket, 5000 * 6); //5*2 seconds timeout.
        Console.WriteLine("Receive：" + result);
        DestroySocket(clientSocket);
      }
      catch (Exception e1)
      {
      }
      return result;
    }
    /// <summary>
    /// 接收数据
    /// </summary>
    /// <param name="socket"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    private static string Receive(Socket socket, int timeout)
    {
      string result = string.Empty;
      socket.ReceiveTimeout = timeout;
      List<byte> data = new List<byte>();
      byte[] buffer = new byte[1024 * 10];
      int length = 0;
      try
      {
        while ((length = socket.Receive(buffer, buffer.Length, 0)) > 0)
        {
          for (int j = 0; j < length; j++)
          {
            data.Add(buffer[j]);
          }
          if (length < buffer.Length)
          {
            break;
          }
        }
      }
      catch (Exception e)
      {
        //throw e;
      }
      if (data.Count > 0)
      {
        result = encode.GetString(data.ToArray(), 0, data.Count);
      }
      return result;
    }
    /// <summary>
    /// 销毁Socket对象
    /// </summary>
    /// <param name="socket"></param>
    private static void DestroySocket(Socket socket)
    {
      if (socket.Connected)
      {
        socket.Shutdown(SocketShutdown.Both);
      }
      socket.Close();
    }
  }
}
