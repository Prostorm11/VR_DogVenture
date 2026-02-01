using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VRDogVenture.Dog
{
    /// <summary>
    /// Helper class to set up the dog companion in a scene.
    /// Attach to an empty GameObject and click "Setup Dog" in the inspector.
    /// </summary>
    public class DogSetupHelper : MonoBehaviour
    {
        [Header("Dog Model (Optional)")]
        [Tooltip("If you have a dog 3D model, assign it here. Otherwise a placeholder will be created.")]
        public GameObject dogModelPrefab;

        [Header("Audio Clips (Optional)")]
        public AudioClip happyBark;
        public AudioClip sadWhimper;
        public AudioClip alertBark;

        /// <summary>
        /// Create a complete dog companion with all required components.
        /// </summary>
        public void SetupDog()
        {
            // Create root object
            GameObject dogRoot = new GameObject("DogCompanion");
            dogRoot.transform.position = transform.position;

            // Add DogCompanion script
            DogCompanion companion = dogRoot.AddComponent<DogCompanion>();

            // Add NavMeshAgent for pathfinding
            NavMeshAgent agent = dogRoot.AddComponent<NavMeshAgent>();
            agent.speed = 1.5f;
            agent.angularSpeed = 500f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.3f;
            agent.radius = 0.3f;
            agent.height = 0.5f;

            // Add AudioSource
            AudioSource audioSource = dogRoot.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 20f;

            // Create visual representation
            GameObject visual;
            if (dogModelPrefab != null)
            {
                visual = Instantiate(dogModelPrefab, dogRoot.transform);
            }
            else
            {
                visual = CreatePlaceholderDog(dogRoot.transform);
            }

            // Get Animator if exists
            Animator animator = visual.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                animator = visual.AddComponent<Animator>();
            }

            // Create bark VFX placeholder
            GameObject vfxObj = new GameObject("BarkWaveVFX");
            vfxObj.transform.SetParent(dogRoot.transform);
            vfxObj.transform.localPosition = new Vector3(0, 0.3f, 0.3f);
            ParticleSystem barkVFX = vfxObj.AddComponent<ParticleSystem>();
            SetupBarkVFX(barkVFX);

            Debug.Log("[DogSetupHelper] Dog companion created! Assign the 'player' reference in DogCompanion to your XR Camera.");
            
#if UNITY_EDITOR
            Selection.activeGameObject = dogRoot;
#endif
        }

        private GameObject CreatePlaceholderDog(Transform parent)
        {
            // Create a simple dog shape using primitives
            GameObject dog = new GameObject("DogPlaceholder");
            dog.transform.SetParent(parent);
            dog.transform.localPosition = Vector3.zero;

            // Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(dog.transform);
            body.transform.localScale = new Vector3(0.25f, 0.15f, 0.3f);
            body.transform.localPosition = new Vector3(0, 0.25f, 0);
            body.transform.localRotation = Quaternion.Euler(0, 0, 90);
            SetDogColor(body, new Color(0.6f, 0.4f, 0.2f)); // Brown

            // Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(dog.transform);
            head.transform.localScale = new Vector3(0.18f, 0.15f, 0.2f);
            head.transform.localPosition = new Vector3(0, 0.3f, 0.22f);
            SetDogColor(head, new Color(0.6f, 0.4f, 0.2f));

            // Snout
            GameObject snout = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            snout.name = "Snout";
            snout.transform.SetParent(head.transform);
            snout.transform.localScale = new Vector3(0.4f, 0.3f, 0.6f);
            snout.transform.localPosition = new Vector3(0, -0.2f, 0.4f);
            snout.transform.localRotation = Quaternion.Euler(90, 0, 0);
            SetDogColor(snout, new Color(0.55f, 0.35f, 0.18f));

            // Nose
            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nose.name = "Nose";
            nose.transform.SetParent(snout.transform);
            nose.transform.localScale = new Vector3(0.4f, 0.4f, 0.3f);
            nose.transform.localPosition = new Vector3(0, 0, 0.5f);
            SetDogColor(nose, Color.black);

            // Ears
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject ear = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ear.name = side < 0 ? "LeftEar" : "RightEar";
                ear.transform.SetParent(head.transform);
                ear.transform.localScale = new Vector3(0.3f, 0.5f, 0.2f);
                ear.transform.localPosition = new Vector3(side * 0.4f, 0.3f, -0.1f);
                SetDogColor(ear, new Color(0.5f, 0.3f, 0.15f));
            }

            // Eyes
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = side < 0 ? "LeftEye" : "RightEye";
                eye.transform.SetParent(head.transform);
                eye.transform.localScale = new Vector3(0.15f, 0.15f, 0.1f);
                eye.transform.localPosition = new Vector3(side * 0.25f, 0.1f, 0.4f);
                SetDogColor(eye, new Color(0.1f, 0.05f, 0f));
            }

            // Tail
            GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            tail.name = "Tail";
            tail.transform.SetParent(dog.transform);
            tail.transform.localScale = new Vector3(0.05f, 0.12f, 0.05f);
            tail.transform.localPosition = new Vector3(0, 0.35f, -0.22f);
            tail.transform.localRotation = Quaternion.Euler(-45, 0, 0);
            SetDogColor(tail, new Color(0.6f, 0.4f, 0.2f));
            tail.AddComponent<TailWag>(); // Add tail wagging animation

            // Legs
            Vector3[] legPositions = {
                new Vector3(-0.08f, 0.1f, 0.12f),
                new Vector3(0.08f, 0.1f, 0.12f),
                new Vector3(-0.08f, 0.1f, -0.12f),
                new Vector3(0.08f, 0.1f, -0.12f)
            };

            for (int i = 0; i < 4; i++)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                leg.name = $"Leg_{i}";
                leg.transform.SetParent(dog.transform);
                leg.transform.localScale = new Vector3(0.06f, 0.1f, 0.06f);
                leg.transform.localPosition = legPositions[i];
                SetDogColor(leg, new Color(0.55f, 0.35f, 0.18f));
            }

            // Remove all colliders from visual parts
            foreach (Collider col in dog.GetComponentsInChildren<Collider>())
            {
                Destroy(col);
            }

            // Add a single capsule collider for the whole dog
            CapsuleCollider mainCollider = dog.AddComponent<CapsuleCollider>();
            mainCollider.center = new Vector3(0, 0.25f, 0);
            mainCollider.radius = 0.15f;
            mainCollider.height = 0.5f;

            return dog;
        }

        private void SetDogColor(GameObject obj, Color color)
        {
            Renderer rend = obj.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat.shader == null)
                    mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                rend.material = mat;
            }
        }

        private void SetupBarkVFX(ParticleSystem ps)
        {
            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = 0.5f;
            main.startSpeed = 5f;
            main.startSize = 0.5f;
            main.startColor = new Color(1f, 0.9f, 0.5f, 0.3f);
            main.maxParticles = 50;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
            shape.radius = 0.1f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.yellow, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            ps.Stop();
        }
    }

    /// <summary>
    /// Simple tail wagging animation.
    /// </summary>
    public class TailWag : MonoBehaviour
    {
        public float wagSpeed = 8f;
        public float wagAmount = 30f;
        private float baseRotation;

        private void Start()
        {
            baseRotation = transform.localRotation.eulerAngles.z;
        }

        private void Update()
        {
            float wag = Mathf.Sin(Time.time * wagSpeed) * wagAmount;
            transform.localRotation = Quaternion.Euler(
                transform.localRotation.eulerAngles.x,
                wag,
                transform.localRotation.eulerAngles.z
            );
        }
    }
}
