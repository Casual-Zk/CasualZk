using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Nethereum.Unity.Rpc;          // for GetUnityRpcRequestClientFactory

public class ChainManager : MonoBehaviour
{
    public static ChainManager instance;

    // --- Essentials --- //
    string zkMainnetRPC = "https://mainnet.era.zksync.io";
    string zkTestnetRPC = "https://testnet.era.zksync.dev";
    string tokenAddress = "0x2B0758ee301AF7AA2C5fA9B7060648fbB2D0dDBD";
    string itemAddress = "0xa2B1aD5a0c739A4AbDd9943cF2cA0AE3ad90E67A";
    string treasuryAddress = "0xA10c223751b208BF18dc0CA9e087B0577fE5b6A8";
    BigInteger zkTestnetID = 280;
    BigInteger zkMainnetID = 280;

    /*
     *  TESTNET
     *  Name: zkSync Era Testnet
     *  RPC: https://testnet.era.zksync.dev
     *  ID: 280
     *  Currency ETH
     *  Explorer: https://goerli.explorer.zksync.io/
     *  
     *  MAINNET
     *  Name: zkSync Era Mainnet
     *  RPC: https://mainnet.era.zksync.io
     *  ID: 324
     *  Currency ETH
     *  Explorer: https://explorer.zksync.io/  
     *  
     */

    public IEnumerator GetWeaponBalances(string address)
    {
        Debug.Log("Getting weapon balances for " + address);
        List<string> addresses = new List<string>();
        List<BigInteger> ids = new List<BigInteger>();

        for (int i = 0; i < 9; i++)
        {
            addresses.Add(address);
            ids.Add(i);
        }

        var queryRequest = new QueryUnityRequest<
            PrivateContracts.Contracts.SampleERC1155.ContractDefinition.BalanceOfBatchFunction,
            PrivateContracts.Contracts.SampleERC1155.ContractDefinition.BalanceOfBatchOutputDTO>(
            zkTestnetRPC, address
        );

        yield return queryRequest.Query(new PrivateContracts.Contracts.SampleERC1155.ContractDefinition
            .BalanceOfBatchFunction()
        {
            Accounts = addresses,
            Ids = ids
        }, itemAddress);

        //Getting the dto response already decoded
        List<BigInteger> balances = queryRequest.Result.ReturnValue1;
        FindObjectOfType<FirebaseDataManager>().OnWeaponBalanceReturn(balances);

        foreach(BigInteger balance in balances) { Debug.Log("Balance: " + balance); }
    }
}