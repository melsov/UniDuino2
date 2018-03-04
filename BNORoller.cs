using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BNORoller : BNOClient {

    [SerializeField]
    float torqueStrength = .2f;
    [SerializeField]
    float velScale = .01f;

    protected override void handleBNOData(BNOData bNOData) {
        rb.AddTorque(bNOData.quat.eulerAngles * torqueStrength * rb.mass, ForceMode.Force);
        rb.AddForce(rb.velocity * velScale);
    }
}
