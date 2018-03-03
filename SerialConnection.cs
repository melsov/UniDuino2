using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using UnityEngine;

public class SerialConnection : MonoBehaviour
{
    [SerializeField, Header("Copy from Arduino IDE: Tools>Port")]
    private string portName = "COM6";
    private SerialPort stream;
    [SerializeField]
    private int baudRate = 115200;

    List<Action<string>> subscribers = new List<Action<string>>();

    public void subscribe(Action<string> a) { subscribers.Add(a); }
    public void unsubscribe(Action<string> a) { subscribers.Remove(a); }

    public void Awake() {
        stream = new SerialPort(portName, baudRate);
        StartCoroutine(readDuino((string s) =>
        {
            foreach(var action in subscribers) {
                if(action != null) { action(s); }
            }
        }));
    }

    public void OnEnable() {
        openSerial();
    }

    public void OnDisable() {
        closeSerial();
    }

    public void OnApplicationQuit() {
        closeSerial();
    }

    private void openSerial() {
        if(stream == null) {
            stream = new SerialPort(portName, baudRate);
            stream.ReadTimeout = 50;
        }
        stream.Open();
    }

    private void closeSerial() {
        print("closing");
        if(stream != null) {
            if(stream.IsOpen) {
                print("is open will close");
                stream.Close();
            }
            stream = null;
        }
    }

    public IEnumerator readDuino(Action<string> callback, Action fail = null, float timeoutSeconds = 60f * 5f) {
        float lastReadTimeSeconds = Time.fixedTime;
        string data = null;
        do {
            data = quatReading();
            if (data != null) {
                callback(data);
                lastReadTimeSeconds = Time.fixedTime;
                yield return null;
            }
            else {
                yield return new WaitForFixedUpdate();
            }
        } while (lastReadTimeSeconds + timeoutSeconds > Time.fixedTime);

    }


    private string quatReading() {
        string data = null;
        try {
            data = stream.ReadLine();
        } catch (TimeoutException) {
            print("serial timed out");
        } catch (System.Exception e) {
            print(string.Format("io exception. PORT: {0} . {1} ", portName, e.ToString()));
        }
        return data;
    }
}
