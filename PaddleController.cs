using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;

public class PaddleController : AccelerometerClient
{
    [SerializeField]
    float strength = 10f;

    [SerializeField]
    int accelSampleSize = 50;
    AccelerationGraph graph;
    [SerializeField, Range(0f, 1f)]
    float minNormalizedMagnitude = .3f;

    Vector3 baseEuler;
    [SerializeField]
    private float stablizeForce;
    private float sqrMagCuttoff = .2f;
    private bool canSwing = true;
    [SerializeField]
    private float swingTime = 1.2f;

    private Vector3 _angularVelocity;
    [SerializeField]
    private float sensitivity = 5f;

    private bool isDoneCalibrating;
    private int receivedCount;
    CalibrationData calibration;

    [SerializeField]
    LRGraph xGraph;
    [SerializeField]
    LRGraph yGraph;

    protected override void Start() {
        base.Start();
        baseEuler = rb.rotation.eulerAngles;
        graph = new AccelerationGraph(accelSampleSize, minNormalizedMagnitude);
    }

    private void OnEnable() {
        canSwing = true;
    }

    //CONSIDER: 
    //make no assumptions about absolute pos:
    //only use relative pos of significant runs

    protected override void handleSensor(Vector3 accl) {
        //accl.z = 0; //SparkFun ADXL3xx is 2 axis
        graph.add(accl);
        receivedCount++;
        //addPaddleForce(accl);
        if (isDoneCalibrating) {
            setAngularPosition(accl);
        } else {
            if(receivedCount == graph.size) {
                calibration = graph.calibrate();
                isDoneCalibrating = true;
                print("finished calibrating");
            }
        }
    }

    //Use accl readings to model the breadboard as a paddle
    //Assume: flat side of breadboard is vertical. 
    //SparkFun accl's x is pointing vertically
    //pos Z is normal to the breadboard surface.
    private void addPaddleForce(Vector3 accl) {
        //try naive approach
        Vector3 tally = graph.evaluate();
        float yTilt = normalizedTiltOnAxis(1);
        Vector3 yTorque = Vector3.Lerp(Vector3.up * tally.sqrMagnitude * strength, Vector3.zero, yTilt);

        if (canSwing && tally.sqrMagnitude > sqrMagCuttoff) {
            print("swing");
            rb.AddTorque(yTorque, ForceMode.Impulse);
            StartCoroutine(stabilize());
        }
    }

    private bool isQualifiedRun(int i) {
        RingBuffer<AccelerationGraph.Run> runRing = graph.runBuffer3[i];
        AccelerationGraph.Run lastRun = runRing.last;
        if(lastRun == null) {
            print("last run null");
            return false;
        }
        return lastRun.count > 3;

        //AccelerationGraph.Run sdRun = calibration.runSDS[i];
        //AccelerationGraph.Run avg = calibration.averageRuns[i];
        //return lastRun.count > avg.count && Mathf.Abs(lastRun.delta) > Mathf.Abs(sdRun.delta);
    }

    private Vector3 applyConditioner(Vector3 accl) {
        return calibration.getDeviation(accl);// (accl - calibration.average).divideBy(calibration.minMax3.spread);
    }

    private void setAngularPosition(Vector3 accl) {
        Vector3 conditioned = accl; // applyConditioner(accl);
        Vector3 scaledData = conditioned.divideBy(calibration.minMax3.max);
        print("scaled: " + scaledData.ToString());
        xGraph.setData(graph.readings.cursor, scaledData.x); // conditioned.x * xGraph.dimensions.y / calibration.minMax3.max.x / 4f);
        yGraph.setData(graph.readings.cursor, scaledData.y); // conditioned.y * yGraph.dimensions.y / calibration.minMax3.max.y / 4f);

        bool qualified = false;
        for(int i=0; i< 3; ++i) {
            if(isQualifiedRun(i)) {
                qualified = true;
                break;
            }
        }
        if(!qualified) {
            conditioned = Vector3.zero;
        }

        Vector3 nextAVelocity = _angularVelocity + conditioned;
        Vector3 avg = (nextAVelocity + _angularVelocity) / 2f;
        TimeStampVec3 adelta = graph.lastDelta();
        Quaternion dQ = Quaternion.Euler(avg * adelta.timestamp);
        Quaternion ro = rb.rotation * dQ;
        rb.MoveRotation(ro);
        _angularVelocity = nextAVelocity;
    }

    private Vector3 clampYEulers() {
        Vector3 eul = eulers();
        eul.y = Mathf.Clamp(eul.y - baseEuler.y, -120f, 120f) + baseEuler.y;
        return eul;
    }

    private IEnumerator stabilize() {

        if (canSwing) {
            canSwing = false;
            int frames = Mathf.RoundToInt(swingTime / Time.fixedDeltaTime);
            for (int i = 0; i < frames; ++i) {
                //rb.rotation = Quaternion.Euler(clampYEulers());
                yield return new WaitForFixedUpdate();
            }
            //Vector3 stabilze;
            //do {
            //    stabilze = rb.rotation.eulerAngles - baseEuler;
            //    rb.AddTorque(rb.angularVelocity * -1f * stablizeForce * Time.fixedDeltaTime, ForceMode.Impulse);
            //    yield return new WaitForFixedUpdate();
            //} while (stabilze.sqrMagnitude > Mathf.Epsilon);

            yield return new WaitForSeconds(.1f);

            rb.rotation = Quaternion.Euler(baseEuler);
            rb.angularVelocity = Vector3.zero;
            canSwing = true;
            print("done stabilizing");
            graph.clear();
        }
    }

    private Vector3 eulers() { return rb.rotation.eulerAngles; }

    private float normalizedTiltOnAxis(int axis) {
        Vector3 eul = eulers();
        float dif = eul[axis] - baseEuler[axis];
        return Mathf.Clamp(Mathf.Abs(dif), 0f, 180f) / 180f;
    }

    protected override void FixedUpdate() {
        base.FixedUpdate();

    }
}

public struct MinMax
{
    public float min, max;

    public static MinMax Create() {
        MinMax mm = new MinMax();
        mm.min = float.MaxValue;
        mm.max = float.MinValue;
        return mm;
    }

    public void add(float f) {
        if(f < min) {
            min = f;
        }
        if(f > max) {
            max = f;
        }
    }

    public float spread { get { return max - min; } }

    public float median { get { return min + spread / 2f; } }

    public float scaled(float f) {
        return (f - min) / spread;
    }

    public override string ToString() {
        return string.Format("MinMax: min {0}, max {1}", min, max);
    }
}

public struct MinMax3
{
    public MinMax x, y, z;

    public static MinMax3 Create() {
        MinMax3 mmm = new MinMax3();
        mmm.x = MinMax.Create();
        mmm.y = MinMax.Create();
        mmm.z = MinMax.Create();
        return mmm;
    }

    public void add(Vector3 v) {
        x.add(v.x); y.add(v.y); z.add(v.z);
    }

    public Vector3 spread {
        get {
            return new Vector3(x.spread, y.spread, z.spread);
        }
    }

    public Vector3 median {
        get {
            return min + spread / 2f;
        }
    }

    public Vector3 min {
        get { return new Vector3(x.min, y.min, z.min); }
    }

    public Vector3 max {
        get { return new Vector3(x.max, y.max, z.max); }
    }

    public Vector3 scaled(Vector3 v) {
        return (v - min).divideBy(spread);
    }

    public override string ToString() {
        return string.Format("MinMax3 x: {0}, y: {1}, z {2}", x.ToString(), y.ToString(), z.ToString());
    }
}

public struct TimeStampVec3
{
    public Vector3 v;
    public float timestamp;

    public static implicit operator Vector3(TimeStampVec3 t) { return t.v; }
    public static implicit operator TimeStampVec3(Vector3 v) { return new TimeStampVec3() { v = v, timestamp = Time.time }; }

    public static TimeStampVec3 operator -(TimeStampVec3 a, TimeStampVec3 b) {
        return new TimeStampVec3() { v = a.v - b.v, timestamp = a.timestamp - b.timestamp };
    }
        
}

public struct CalibrationData
{
    public Vector3 average;
    public MinMax3 minMax3;
    public Vector3 standardDeviation;
    public AccelerationGraph.Run3 averageRuns;
    public AccelerationGraph.Run3 runSDS;

    public Vector3 getDeviation(Vector3 v) {
        Vector3 result = v - average;
        if(v.sqrMagnitude <= standardDeviation.sqrMagnitude) {
            return Vector3.zero;
        }
        return result;
    }

    public Vector3 getNormalizedDeviation(Vector3 v) {
        return getDeviation(v).divideBy(minMax3.spread/2f);
    }

    public override string ToString() {
        return string.Format("CalbrData: average: {0} . minMax {1} . standardDev: {2} . avgRuns {3} , runSDS {4}", 
            average.ToString(), minMax3.ToString(), standardDeviation.ToString(), averageRuns.ToString(), runSDS.ToString());
    }
}

public class AccelerationGraph
{
    public class Run
    {
        public int count { get; private set; }
        public float first { get; private set; }
        public float last { get; private set; }

        public Run() {
            count = 0; first = 0; last = 0;
        }

        public float Sign { get { return Mathf.Sign(delta); } }
        public float delta { get { return last - first; } }
        public float slope { get { return delta / count; } }

        public bool incorporate(float f) {
            if(count == 0) {
                first = f;
                last = first;
                count++;
                return true;
            }
            if(count == 1) {
                last = f;
                count++;
                return true;
            }
            if(Sign == Mathf.Sign(f - last)) {
                last = f;
                count++;
                return true;
            }
            return false;
        }

        public Run squared() { return this * this; }

        public Run sqrt() {
            return new Run() { count = Mathf.RoundToInt(Mathf.Sqrt(count)), first = Mathf.Sqrt(first), last = Mathf.Sqrt(last) };
        }

        public static Run operator+(Run a, Run b) {
            return new Run() { count = a.count + b.count, first = a.first + b.first, last = a.last + b.last };
        }

        public static Run operator -(Run a, Run b) {
            return new Run() { count = a.count - b.count, first = a.first - b.first, last = a.last - b.last };
        }

        public static Run operator*(Run a, Run b) {
            return new Run() { count = a.count * b.count, first = a.first * b.first, last = a.last * b.last };
        }

        public static Run operator*(Run a, float f) {
            return new Run() { count = Mathf.RoundToInt(a.count * f), first = a.first * f, last = a.last * f };
        }

        public override string ToString() {
            return string.Format("Run: count: {0} | first: {1} | last: {2}", count, first, last);
        }
    }

    public struct Run3
    {
        Run[] xyz;

        public Run3(params Run[] xyz) {
            this.xyz = xyz;
        }

        public Run this[int i] {
            get { return xyz[i]; }
        }

        public static implicit operator Run3(Run[] runs) { return new Run3() { xyz = runs }; }

        public override string ToString() {
            return string.Format("x: {0} y: {1} z: {2}", xyz[0], xyz[1], xyz[2]);
        }
    }

    public struct RunBuffer3
    {
        public RingBuffer<Run>[] runRings { get; private set; }

        public RunBuffer3(int size) {
            runRings = new RingBuffer<Run>[3];
            foreach(int i in Enumerable.Range(0,3)) {
                runRings[i] = new RingBuffer<Run>(size);
            }
        }

        public RingBuffer<Run> x { get { return runRings[0]; } }
        public RingBuffer<Run> y { get { return runRings[1]; } }
        public RingBuffer<Run> z { get { return runRings[2]; } }

        public RingBuffer<Run> this[int i] {
            get { return runRings[i]; }
        }

        private void addTo(int index, Vector3 v) {
            if(runRings[index].last == null) {
                runRings[index].last = new Run();
            }
            Run run = runRings[index].last;
            if (!run.incorporate(v[index])) {
                Run next = new Run();
                next.incorporate(v[index]);
                runRings[index].push(next);
            }
        }

        public void add(Vector3 v) {
            for(int i=0; i < 3; ++i) {
                addTo(i, v);
            }
        }

        private Run averageRun(int index) {
            Run avg = new Run();
            RingBuffer<Run> runRing = runRings[index];
            for(int i= 0; i < runRing.count; ++i) {
                if(runRing[i] == null) { continue; }
                avg += runRing[i];
            }
            return avg * (1f / runRing.count);
        }

        public Run[] averageRuns() {
            Run[] avg = new Run[3];
            for(int i = 0; i < 3; ++i) {
                avg[i] = averageRun(i);
            }
            return avg;
        }

        private Run standardDeviationAgainst(int index, Run avg) {
            Run sd = new Run();
            RingBuffer<Run> runRing = runRings[index];
            for(int i=0; i< runRing.count;++i) {
                if(runRing[i] == null) { continue; }
                sd += (runRing[i] - avg).squared();
            }
            return (sd * (1f / runRing.count)).sqrt();
        }

        public Run[] standardDeviation(Run[] average) {
            Run[] sds = new Run[3];
            for(int i = 0; i < 3; ++i) {
                sds[i] = standardDeviationAgainst(i, average[i]);
            }
            return sds;
        }
    }

    public RingBuffer<TimeStampVec3> readings { get; private set; }
    private MinMax minMax;
    private List<Vector3> tally;
    float minNormalizedMagnitude;
    public RunBuffer3 runBuffer3 { get; private set; }

    public AccelerationGraph(int size = 50, float minNormalizedMagnitude = .3f) {
        readings = new RingBuffer<TimeStampVec3>(size);
        runBuffer3 = new RunBuffer3(size);
        minMax = MinMax.Create();
        this.minNormalizedMagnitude = minNormalizedMagnitude;
        tally = new List<Vector3>(size);
    }

    public void add(Vector3 v) {
        minMax.add(v.sqrMagnitude);
        readings.push(v);
        runBuffer3.add(v);
    }

    public Vector3 evaluate() {
        Vector3 result = Vector3.zero;
        int count = 0;
        foreach (Vector3 v in readings.getValues()) {
            if (minMax.scaled(v.sqrMagnitude) > minNormalizedMagnitude) {
                count++;
                result += v;
            }
        }

        if (count > 0) {
            result /= count;
        }
        else {
            return Vector3.zero;
        }

        return result;
    }

    public CalibrationData calibrate() {
        Vector3 avg = Vector3.zero;
        MinMax3 mmm = MinMax3.Create();
        List<Vector3> data = new List<Vector3>(size);
        foreach (Vector3 v in readings.getValues()) {
            data.Add(v);
        }
        data.Sort((a, b) => { return a.sqrMagnitude.CompareTo(b.sqrMagnitude); });

        //average & minMax
        int samples = (int)(data.Count * 0.88f);
        Vector3 deviaton = Vector3.zero;
        for(int i=0; i < samples; ++i) {
            mmm.add(data[i]);
            avg += data[i];
        }
        avg /= samples;

        //standard deviation
        for (int i = 0; i < samples; ++i) {
            deviaton += (data[i] - avg).componentsSquared();
        }
        deviaton = (deviaton / samples).componentsSqrt();

        //runs
        Run[] avgRuns = runBuffer3.averageRuns();
        Run[] runStandardDeviations = runBuffer3.standardDeviation(avgRuns);

        CalibrationData cd = new CalibrationData() { average = avg, minMax3 = mmm, standardDeviation = deviaton, averageRuns = avgRuns, runSDS = runStandardDeviations };

        Debug.Log(cd.ToString());
        EditorApplication.isPaused = true;
        return cd;
    }

    public void clear() {
        for(int i=0; i<readings.size; ++i) {
            readings.push(Vector3.zero);
        }
    }

    public TimeStampVec3 lastDelta() {
        return readings.last - readings[readings.size - 2];
    }

    public int size { get { return readings.size; } }


}
