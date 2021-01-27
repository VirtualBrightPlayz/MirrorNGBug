using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Mirror;
using UnityEngine;

public class BuggedNetAuth : NetworkAuthenticator
{
    public string authPassword;

    public struct PasswordMessage
    {
        public string password;
    }

    public struct AcceptPasswordMessage
    {
        public bool allowed;
    }

    void DummyWrite(NetworkWriter writer)
    {
        writer.Write(new PasswordMessage());
        writer.Write(new AcceptPasswordMessage());
    }

    public async UniTask DisconnectLate(INetworkConnection conn, float time)
    {
        await UniTask.Delay((int)(time * 1000));
        conn.Send(new AcceptPasswordMessage()
        {
            allowed = false
        });
        conn.Disconnect();
    }

    public override void OnServerAuthenticate(INetworkConnection conn)
    {
        conn.RegisterHandler<PasswordMessage>(PasswordMessageHandler);
        UniTask.Create(() => DisconnectLate(conn, 10f));
    }

    private void PasswordMessageHandler(INetworkConnection conn, PasswordMessage msg)
    {
        if (msg.password == authPassword)
        {
            conn.Send(new AcceptPasswordMessage()
            {
                allowed = true
            });
            base.OnServerAuthenticate(conn);
        }
    }

    public override void OnClientAuthenticate(INetworkConnection conn)
    {
        conn.RegisterHandler<AcceptPasswordMessage>(AcceptPasswordMessageHandler);
        conn.Send(new PasswordMessage()
        {
            password = authPassword
        });
    }

    private void AcceptPasswordMessageHandler(INetworkConnection conn, AcceptPasswordMessage msg)
    {
        Debug.LogError("Due to the bug, this line will never run. If you see this in the console, the bug has been fixed.");
        if (!msg.allowed)
        {
            conn.Disconnect();
            return;
        }
        base.OnClientAuthenticate(conn);
    }
}
