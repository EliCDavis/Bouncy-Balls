using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class GameManager : MonoBehaviour
{
    enum DistributionType
    {
        Grid,
        Latice
    }

    private GameObject pointCollection;

    private float timeStarted;

    private float startingVelocity;

    [SerializeField]
    private PhysicsMaterial2D physicsMaterial;

    [SerializeField]
    private GameObject particlePrefab;

    [SerializeField]
    private GameObject borderPrefab;

    [SerializeField]
    private Color[] colors;

    [SerializeField]
    private Camera camera;

    [SerializeField]
    private InputField numberParticlesInput;

    [SerializeField]
    private InputField particleSizeInput;

    [SerializeField]
    private InputField wallSizeInput;

    [SerializeField]
    private InputField initalParticleVelocityInput;

    [SerializeField]
    private Text timeDisplay;

    [SerializeField]
    private Dropdown distributionDropdown;

    [SerializeField]
    private Toggle enforceVelocityToggle;

    [SerializeField]
    private Toggle enforceFriction;

    [SerializeField]
    private Button playPauseButton;

    [SerializeField]
    private Material lineRendererMaterial;

    [SerializeField]
    private Font guiFont;

    private int GetNumberOfParticlesFromInput()
    {
        return int.Parse(this.numberParticlesInput.text);
    }

    private float GetParticleSizeFromInput()
    {
        return float.Parse(this.particleSizeInput.text);
    }

    private float GetVelocityFromInput()
    {
        return float.Parse(this.initalParticleVelocityInput.text);
    }

    private float GetWallSizeFromInput()
    {
        return float.Parse(this.wallSizeInput.text);
    }

    private bool EnforceStartingVelocity()
    {
        if (enforceVelocityToggle == null)
        {
            return false;
        }
        return enforceVelocityToggle.isOn;
    }

    private DistributionType GetDistributionTypeFromInput()
    {
        if (this.distributionDropdown == null)
        {
            return DistributionType.Grid;
        }

        if (this.distributionDropdown.options[this.distributionDropdown.value].text == "Grid")
        {
            return DistributionType.Grid;
        }

        return DistributionType.Latice;
    }

    void Start()
    {
        DebugGUI.font = guiFont;
        DebugGUI.Instance.drawInBuild = true;
        DebugGUI.Instance.displayGraphs = true;
        Run();
    }

    public void ToggleTimeScale()
    {
        if (Time.timeScale == 0)
        {
            playPauseButton.GetComponentInChildren<Text>().text = "Pause";
            Time.timeScale = 1;
        }
        else if (Time.timeScale == 1)
        {
            playPauseButton.GetComponentInChildren<Text>().text = "Play";
            Time.timeScale = 0;
        }
    }

    public void Run()
    {
        if (pointCollection != null)
        {
            Destroy(pointCollection);
        }
        startingVelocity = GetVelocityFromInput();
        DebugGUI.SetGraphProperties("avgVelocity", "Average Velocity", 0, startingVelocity, 1, Color.black, true);

        if (GetDistributionTypeFromInput() == DistributionType.Grid)
        {
            pointCollection = BuildPoints(
                        GetNumberOfParticlesFromInput(),
                        GetParticleSizeFromInput(),
                        GetWallSizeFromInput(),
                        startingVelocity
                    );
        }
        else if (GetDistributionTypeFromInput() == DistributionType.Latice)
        {
            pointCollection = BuildPointsLatice(
                        GetNumberOfParticlesFromInput(),
                        GetParticleSizeFromInput(),
                        GetWallSizeFromInput(),
                        startingVelocity
                    );
        }

        timeStarted = Time.time;
    }

    bool lastFrameEnforcedFriction = false;

    void Update()
    {

        this.timeDisplay.text = (Time.time - timeStarted).ToString("0.00");

        var particles = pointCollection.transform.Find("Points").GetComponentsInChildren<Rigidbody2D>();
        float totalVelocity = 0;
        foreach (var p in particles)
        {
            if (pointCollection != null && EnforceStartingVelocity())
            {
                p.velocity = p.velocity.normalized * this.startingVelocity;
            }

            totalVelocity += p.velocity.magnitude;
        }

        DebugGUI.Graph("avgVelocity", totalVelocity / particles.Length);



        var currentFriction = enforceFriction.isOn;
        if (lastFrameEnforcedFriction != currentFriction)
        {
            if (currentFriction)
            {
                foreach (var p in particles)
                {
                    p.sharedMaterial.friction = 1;
                    p.sharedMaterial = p.sharedMaterial;
                }
            }
            else
            {
                foreach (var p in particles)
                {
                    p.sharedMaterial.friction = 0;
                    p.sharedMaterial = p.sharedMaterial;
                }
            }
        }

        lastFrameEnforcedFriction = currentFriction;
    }

    private GameObject BuildCircularContainer(Vector2 center, float radius)
    {
        GameObject container = new GameObject("Container");
        container.transform.position = new Vector3(center.x, center.y, 0);

        var edgeCollider = container.AddComponent<EdgeCollider2D>();
        var lineRenderer = container.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.material = this.lineRendererMaterial;

        int pointsOnCircle = 128;

        var points = new Vector2[pointsOnCircle];
        var points3 = new Vector3[pointsOnCircle];
        var radAndAHalf = (radius + .5f);
        for (int i = 0; i < pointsOnCircle - 1; i++)
        {
            var angle = (i / (float)pointsOnCircle) * Mathf.PI * 2;
            points[i] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            points3[i] = new Vector3(Mathf.Cos(angle) * radAndAHalf, Mathf.Sin(angle) * radAndAHalf, 0);
        }
        points[pointsOnCircle - 1] = new Vector2(Mathf.Cos(0) * radius, Mathf.Sin(0) * radius);
        points3[pointsOnCircle - 1] = new Vector3(Mathf.Cos(0) * radAndAHalf, Mathf.Sin(0) * radAndAHalf, 0);
        lineRenderer.positionCount = pointsOnCircle;
        lineRenderer.SetPositions(points3);
        edgeCollider.points = points;

        return container;
    }

    private GameObject BuildContainer(Vector2 bottomLeft, float sideSize)
    {
        var container = new GameObject("Container");
        container.transform.position = new Vector3(bottomLeft.x + (sideSize / 2), bottomLeft.y + (sideSize / 2), 0);

        var bottom = Instantiate(borderPrefab, new Vector3(
          bottomLeft.x + (sideSize / 2f),
          bottomLeft.y,
          0
        ), Quaternion.identity);
        bottom.transform.localScale = new Vector3(sideSize + 2, 1, 1);
        bottom.transform.SetParent(container.transform);

        var top = Instantiate(borderPrefab, new Vector3(
            bottomLeft.x + (sideSize / 2f),
            bottomLeft.y + sideSize,
            0
        ), Quaternion.identity);
        top.transform.localScale = new Vector3(sideSize + 2, 1, 1);
        top.transform.SetParent(container.transform);

        var left = Instantiate(borderPrefab, new Vector3(
            bottomLeft.x - .5f,
            bottomLeft.y + (sideSize / 2f),
            0
        ), Quaternion.identity);
        left.transform.localScale = new Vector3(1, sideSize + 1, 1);
        left.transform.SetParent(container.transform);

        var right = Instantiate(borderPrefab, new Vector3(
            bottomLeft.x + sideSize + .5f,
            bottomLeft.y + (sideSize / 2f),
            0
        ), Quaternion.identity);
        right.transform.localScale = new Vector3(1, sideSize + 1, 1);
        right.transform.SetParent(container.transform);

        return container;
    }



    private GameObject BuildPoints(int numberOfPoints, float pointSize, float cubeWallSize, float initialVelocity)
    {
        var results = new GameObject("Sim");
        var pointResults = new GameObject("Points");

        int sqr = Mathf.CeilToInt(Mathf.Sqrt(numberOfPoints));
        float spacing = cubeWallSize / (sqr);

        for (int i = 0; i < numberOfPoints; i++)
        {
            GameObject point = Instantiate(particlePrefab);
            point.transform.name = string.Format("Point: {0}", i);
            point.transform.position = new Vector3(
               ((i % sqr) * spacing),
               (Mathf.Floor(i / sqr) * spacing),
                0
            );
            point.transform.SetParent(pointResults.transform);
            point.transform.localScale *= pointSize;
            point.GetComponent<MeshRenderer>().material.color = GetRandomColor();

            var rb2 = point.GetComponent<Rigidbody2D>();
            rb2.velocity = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * initialVelocity;
        }

        pointResults.transform.SetParent(results.transform);

        var container = BuildContainer(Vector2.one * (-spacing / 2.0f), cubeWallSize);
        container.transform.SetParent(results.transform);

        camera.transform.position = new Vector3(container.transform.position.x, container.transform.position.y, -10);
        camera.orthographicSize = cubeWallSize * .6f;
        return results;
    }

    private GameObject BuildPointsLatice(int numberOfPoints, float pointSize, float cubeWallSize, float initialVelocity)
    {
        /*
        n=100;

        # eg default radius of disk
        diskRadius = 0.4/Sqrt[n];

        goldenRatio = (1+Sqrt[5])/2.0;

        rng = Range@n; 
        (* if you are using zero-based arrays add 0.5 instead of subtracting*) 
        r = Sqrt[rng + 0.5]/n;

        # shrink by a tiny factor to ensure that the entirety of every circle lies in the interior of a unit circle
        r = (1-diskRadius) *  r;
        theta = 2Pi/goldenRatio * rng ;

        (* note that this is element-wise array multiplication coz r and theta are n-size arrays
        x = r * Cos[theta];
        y = r* Sin[theta];
        */

        var results = new GameObject("Sim");
        var pointResults = new GameObject("Points");
        float diskRadius = 0.4f / Mathf.Sqrt(numberOfPoints); //2.0f / Mathf.Sqrt(numberOfPoints);

        for (float i = 0; i < numberOfPoints; i += 1.0f)
        {
            GameObject point = Instantiate(particlePrefab);
            point.transform.name = string.Format("Point: {0}", i);

            float r = Mathf.Sqrt((i + 0.5f) / numberOfPoints);
            // r = (1 - diskRadius) * r;
            float theta = Mathf.PI * (1f + Mathf.Sqrt(5f)) * (i + .5f);

            point.transform.position = new Vector3(
               r * Mathf.Cos(theta),
               r * Mathf.Sin(theta),
                0
            );

            point.transform.localScale *= diskRadius * 2;
            point.transform.SetParent(pointResults.transform);
            point.GetComponent<MeshRenderer>().material.color = GetRandomColor();

            var rb2 = point.GetComponent<Rigidbody2D>();
            rb2.velocity = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * initialVelocity;
        }

        pointResults.transform.localScale *= pointSize / (diskRadius * 2f);

        var container = BuildCircularContainer(Vector2.one * (cubeWallSize / 2.0f), cubeWallSize / 2.0f);

        camera.transform.position = new Vector3(container.transform.position.x, container.transform.position.y, -10);
        pointResults.transform.position = new Vector3(container.transform.position.x, container.transform.position.y, 0);
        pointResults.transform.SetParent(results.transform);
        container.transform.SetParent(results.transform);
        camera.orthographicSize = cubeWallSize * .6f;
        return results;
    }

    private Color GetRandomColor()
    {
        if (colors == null || colors.Length == 0)
        {
            return Color.green;
        }
        return colors[Random.Range(0, colors.Length - 1)];
    }


}
