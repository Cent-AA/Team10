using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ArenaCamera : MonoBehaviour
{
    private enum SplitState
    {
        Shared,
        Splitting,
        Split,
        Merging
    }

    [Header("Targets")]
    public Transform target1;
    public Transform target2;

    [Header("Shared Camera")]
    public float followSmoothness = 5f;
    public Vector2 offset = Vector2.zero;

    [Header("Zoom")]
    public float minZoom = 4f;
    public float maxZoom = 8f;
    public float zoomSmoothness = 3f;
    public float zoomPadding = 2f;

    [Header("Split Cameras")]
    public bool enableSplitScreen = true;
    public Camera leftSplitCamera;
    [FormerlySerializedAs("secondPlayerCamera")] public Camera rightSplitCamera;
    [FormerlySerializedAs("createSecondCameraAutomatically")] public bool createSplitCamerasAutomatically = true;
    [FormerlySerializedAs("splitDistance")] public float splitThreshold = 12f;
    [FormerlySerializedAs("mergeDistance")] public float mergeThreshold = 9f;
    public float splitDuration = 1.2f;
    public AnimationCurve splitEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float splitFollowSmoothness = 7f;
    public float splitZoom = 5f;
    public Vector2 splitDeadZone = new Vector2(1.5f, 1f);
    public bool playerOneOnRight = true;

    [Header("Split Divider")]
    public bool drawScreenDivider = true;
    public Image dividerImage;
    public bool createDividerAutomatically = true;
    public int dividerSortingOrder = 1000;
    public Color dividerColor = new Color(0f, 0f, 0f, 0.9f);
    public bool animateDividerWidth = false;
    public float dividerLineWidth = 3f;
    public float dividerMaxWidth = 3f;

    [Header("Shake")]
    public bool cameraShakeEnabled = true;
    public bool disableShakeDuringSplitScreen = true;
    [Range(1f, 10f)] public float shakeIntensityLevel = 3f;
    [Range(0f, 1f)] public float splitScreenShakeMultiplier = 0.25f;
    public float maxShakeOffset = 0.45f;
    public float shakeDamping = 8f;

    [Header("Map Bounds")]
    public SpriteRenderer mapSprite;

    private Camera sharedCamera;
    private SplitState splitState = SplitState.Shared;
    private Coroutine splitTransitionRoutine;
    private float splitProgress;
    private Vector3 leftSplitPosition;
    private Vector3 rightSplitPosition;
    private Vector3 shakeOffset;
    private bool splitPositionsInitialized;
    private bool createdLeftSplitCamera;
    private bool createdRightSplitCamera;
    private bool createdDividerCanvas;
    private Canvas dividerCanvas;
    private int originalSharedCullingMask;
    private CameraClearFlags originalSharedClearFlags;

    private static float shakeIntensity;
    private static float shakeDuration;
    private static ArenaCamera instance;
    private bool isCinematic = false;
    private Vector3 cinematicTarget;
    private float cinematicZoom;

    public bool IsSplitScreenActive => splitProgress > 0.001f;

    // Спрайт карты, к которому привязаны границы камеры — переиспользуется боссом для своих границ.
    public static SpriteRenderer MapSprite => instance != null ? instance.mapSprite : null;

    void Awake()
    {
        instance = this;
        sharedCamera = GetComponent<Camera>();
        if (sharedCamera != null)
        {
            originalSharedCullingMask = sharedCamera.cullingMask;
            originalSharedClearFlags = sharedCamera.clearFlags;
        }

        EnsureSplitCameras();
        EnsureDivider();
        ApplyCameraLayout();
        UpdateDivider();
    }

    void Start()
    {
        ResolveTargets();
        InitializeSplitPositionsFromSharedCrop();
    }

    void LateUpdate()
    {
        if (isCinematic)
        {
            // Сначала обновляем зум, потом клампим — чтобы ClampToMap считал границы по актуальному размеру.
            if (sharedCamera != null)
                sharedCamera.orthographicSize = Mathf.Lerp(
                    sharedCamera.orthographicSize,
                    cinematicZoom,
                    zoomSmoothness * Time.deltaTime
                );

            Vector3 cinematicPos = Vector3.Lerp(
                transform.position,
                new Vector3(cinematicTarget.x, cinematicTarget.y, transform.position.z),
                followSmoothness * Time.deltaTime
            );
            transform.position = ClampToMap(cinematicPos, sharedCamera);

            UpdateShake();
            transform.position += shakeOffset;
            return;
        }

        if (sharedCamera == null) return;

        ResolveTargets();
        if (target1 == null && target2 == null)
        {
            ForceSharedLayout();
            return;
        }

        EnsureSplitCameras();
        UpdateSplitTrigger();
        UpdateShake();
        UpdateCameraMotion();
        ApplyCameraLayout();
        UpdateDivider();
    }

    void ResolveTargets()
    {
        Registry.CleanupPlayers();

        Transform playerOne = null;
        Transform playerTwo = null;

        for (int i = 0; i < Registry.Players.Count; i++)
        {
            Transform player = Registry.Players[i];
            if (player == null) continue;

            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller == null) controller = player.GetComponentInChildren<PlayerController>();

            if (controller != null)
            {
                if (controller.playerNumber == 1) playerOne = player;
                else if (controller.playerNumber == 2) playerTwo = player;
            }
        }

        if (playerOne == null && Registry.Players.Count > 0)
            playerOne = Registry.Players[0];

        if (playerTwo == null)
        {
            for (int i = 0; i < Registry.Players.Count; i++)
            {
                Transform player = Registry.Players[i];
                if (player != null && player != playerOne)
                {
                    playerTwo = player;
                    break;
                }
            }
        }

        target1 = playerOne;
        target2 = playerTwo;

        if (target1 == null && target2 != null)
        {
            target1 = target2;
            target2 = null;
        }
    }

    void UpdateSplitTrigger()
    {
        if (!enableSplitScreen || target1 == null || target2 == null)
        {
            if (splitState != SplitState.Shared)
                BeginSplitTransition(false);
            return;
        }

        float distance = Vector2.Distance(target1.position, target2.position);

        if ((splitState == SplitState.Shared || splitState == SplitState.Merging) && distance >= splitThreshold)
            BeginSplitTransition(true);
        else if ((splitState == SplitState.Split || splitState == SplitState.Splitting) && distance <= mergeThreshold)
            BeginSplitTransition(false);
    }

    void BeginSplitTransition(bool split)
    {
        SplitState desiredState = split ? SplitState.Splitting : SplitState.Merging;
        if (splitState == desiredState) return;
        if (split && splitState == SplitState.Split) return;
        if (!split && splitState == SplitState.Shared) return;

        if (splitTransitionRoutine != null)
            StopCoroutine(splitTransitionRoutine);

        EnsureSplitCameras();

        if (split)
            InitializeSplitPositionsFromSharedCrop();

        splitTransitionRoutine = StartCoroutine(SplitTransitionRoutine(split));
    }

    IEnumerator SplitTransitionRoutine(bool split)
    {
        splitState = split ? SplitState.Splitting : SplitState.Merging;

        float from = splitProgress;
        float to = split ? 1f : 0f;
        float duration = Mathf.Max(0.01f, splitDuration * Mathf.Abs(to - from));
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float easedTime = splitEase != null ? splitEase.Evaluate(normalizedTime) : normalizedTime;
            splitProgress = Mathf.Lerp(from, to, easedTime);
            yield return null;
        }

        splitProgress = to;
        splitState = split ? SplitState.Split : SplitState.Shared;
        splitTransitionRoutine = null;

        if (!split)
            splitPositionsInitialized = false;
    }

    void EnsureSplitCameras()
    {
        if (!enableSplitScreen || sharedCamera == null) return;

        if (leftSplitCamera == sharedCamera) leftSplitCamera = null;
        if (rightSplitCamera == sharedCamera) rightSplitCamera = null;

        if (leftSplitCamera == null && createSplitCamerasAutomatically)
        {
            leftSplitCamera = CreateSplitCamera("Left Split Camera");
            createdLeftSplitCamera = true;
        }

        if (rightSplitCamera == null && createSplitCamerasAutomatically)
        {
            rightSplitCamera = CreateSplitCamera("Right Split Camera");
            createdRightSplitCamera = true;
        }

        ConfigureSplitCamera(leftSplitCamera);
        ConfigureSplitCamera(rightSplitCamera);
    }

    Camera CreateSplitCamera(string suffix)
    {
        GameObject cameraObject = new GameObject(name + " " + suffix);
        cameraObject.transform.SetParent(transform.parent, false);
        cameraObject.transform.position = transform.position;
        cameraObject.transform.rotation = transform.rotation;

        Camera cameraComponent = cameraObject.AddComponent<Camera>();
        cameraComponent.CopyFrom(sharedCamera);
        cameraComponent.depth = sharedCamera.depth + 1f;
        cameraComponent.enabled = false;
        return cameraComponent;
    }

    void ConfigureSplitCamera(Camera cameraComponent)
    {
        if (cameraComponent == null) return;

        cameraComponent.tag = "Untagged";
        cameraComponent.depth = sharedCamera.depth + 1f;

        AudioListener audioListener = cameraComponent.GetComponent<AudioListener>();
        if (audioListener != null)
            audioListener.enabled = false;
    }

    void ApplyCameraLayout()
    {
        bool showSplit = enableSplitScreen && splitProgress > 0.001f && target1 != null && target2 != null;

        sharedCamera.enabled = true;
        sharedCamera.rect = new Rect(0f, 0f, 1f, 1f);
        sharedCamera.cullingMask = showSplit ? 0 : originalSharedCullingMask;
        sharedCamera.clearFlags = showSplit ? CameraClearFlags.Nothing : originalSharedClearFlags;

        if (leftSplitCamera != null)
        {
            leftSplitCamera.enabled = showSplit;
            leftSplitCamera.rect = new Rect(0f, 0f, 0.5f, 1f);
        }

        if (rightSplitCamera != null)
        {
            rightSplitCamera.enabled = showSplit;
            rightSplitCamera.rect = new Rect(0.5f, 0f, 0.5f, 1f);
        }
    }

    void EnsureDivider()
    {
        if (!drawScreenDivider || dividerImage != null || !createDividerAutomatically) return;

        GameObject canvasObject = new GameObject(name + " Split Divider Canvas");
        dividerCanvas = canvasObject.AddComponent<Canvas>();
        dividerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        dividerCanvas.sortingOrder = dividerSortingOrder;
        canvasObject.AddComponent<CanvasScaler>();

        GameObject imageObject = new GameObject("Split Divider");
        imageObject.transform.SetParent(canvasObject.transform, false);
        dividerImage = imageObject.AddComponent<Image>();
        dividerImage.raycastTarget = false;

        createdDividerCanvas = true;
    }

    void UpdateDivider()
    {
        if (!drawScreenDivider)
        {
            if (dividerImage != null)
                dividerImage.gameObject.SetActive(false);
            return;
        }

        EnsureDivider();
        if (dividerImage == null) return;

        float t = Mathf.Clamp01(splitProgress);
        bool visible = t > 0.001f;
        dividerImage.gameObject.SetActive(visible);
        if (!visible) return;

        float width = animateDividerWidth
            ? Mathf.Lerp(dividerMaxWidth, dividerLineWidth, t)
            : dividerLineWidth;
        float alpha = dividerColor.a * Mathf.Clamp01(t * 2f);
        dividerImage.color = new Color(dividerColor.r, dividerColor.g, dividerColor.b, alpha);

        RectTransform rectTransform = dividerImage.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(width, 0f);
    }

    void UpdateCameraMotion()
    {
        Vector3 sharedPosition = ClampToMap(GetSharedTargetPosition(), sharedCamera);
        float sharedZoom = GetSharedZoom();

        MoveCameraSmooth(transform, sharedCamera, sharedPosition, sharedZoom, followSmoothness);
        Vector3 currentSharedPosition = sharedCamera.transform.position;
        currentSharedPosition.z = transform.position.z;
        float currentSharedZoom = sharedCamera.orthographicSize;

        if (splitProgress <= 0.001f || leftSplitCamera == null || rightSplitCamera == null || target1 == null || target2 == null)
            return;

        Transform leftTarget = GetLeftTarget();
        Transform rightTarget = GetRightTarget();
        if (leftTarget == null || rightTarget == null) return;

        if (!splitPositionsInitialized)
            InitializeSplitPositionsFromSharedCrop();

        leftSplitPosition = UpdateDeadZonePosition(leftTarget, leftSplitPosition, leftSplitCamera);
        rightSplitPosition = UpdateDeadZonePosition(rightTarget, rightSplitPosition, rightSplitCamera);

        float t = Mathf.Clamp01(splitProgress);
        Vector3 leftCropPosition = GetSharedCropPosition(currentSharedPosition, currentSharedZoom, true);
        Vector3 rightCropPosition = GetSharedCropPosition(currentSharedPosition, currentSharedZoom, false);
        Vector3 leftPosition = Vector3.Lerp(leftCropPosition, leftSplitPosition, t);
        Vector3 rightPosition = Vector3.Lerp(rightCropPosition, rightSplitPosition, t);
        float splitTargetZoom = GetSplitZoom(leftSplitCamera);
        float currentSplitZoom = Mathf.Lerp(currentSharedZoom, splitTargetZoom, t);

        SetCameraPose(leftSplitCamera, leftPosition, currentSplitZoom);
        SetCameraPose(rightSplitCamera, rightPosition, currentSplitZoom);
    }

    Transform GetLeftTarget() => playerOneOnRight ? target2 : target1;
    Transform GetRightTarget() => playerOneOnRight ? target1 : target2;

    Vector3 GetSharedTargetPosition()
    {
        Transform target = target1 != null ? target1 : target2;
        if (target2 == null || target1 == null)
            return new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);

        Vector2 center = ((Vector2)target1.position + (Vector2)target2.position) * 0.5f;
        return new Vector3(center.x + offset.x, center.y + offset.y, transform.position.z);
    }

    float GetSharedZoom()
    {
        float targetZoom = minZoom;
        if (target1 != null && target2 != null)
        {
            float fullScreenAspect = Screen.height > 0 ? (float)Screen.width / Screen.height : sharedCamera.aspect;
            float halfWidth = Mathf.Abs(target1.position.x - target2.position.x) * 0.5f;
            float halfHeight = Mathf.Abs(target1.position.y - target2.position.y) * 0.5f;
            float horizontalZoom = halfWidth / Mathf.Max(0.01f, fullScreenAspect);
            float requiredZoom = Mathf.Max(horizontalZoom, halfHeight) + zoomPadding;
            targetZoom = Mathf.Clamp(requiredZoom, minZoom, maxZoom);
        }
        return ClampZoomToMap(targetZoom, sharedCamera);
    }

    Vector3 GetSharedCropPosition(Vector3 sharedPosition, float sharedZoom, bool leftSide)
    {
        float fullScreenAspect = Screen.height > 0 ? (float)Screen.width / Screen.height : sharedCamera.aspect;
        float cropOffset = sharedZoom * fullScreenAspect * 0.5f;
        Vector3 cropPosition = sharedPosition + Vector3.right * (leftSide ? -cropOffset : cropOffset);
        cropPosition.z = transform.position.z;
        return cropPosition;
    }

    Vector3 UpdateDeadZonePosition(Transform player, Vector3 currentPosition, Camera cameraComponent)
    {
        Vector3 desiredPosition = GetDeadZonePosition(player, currentPosition);
        Vector3 nextPosition = Vector3.Lerp(currentPosition, desiredPosition, splitFollowSmoothness * Time.deltaTime);
        return ClampToMap(nextPosition, cameraComponent);
    }

    Vector3 GetDeadZonePosition(Transform player, Vector3 currentPosition)
    {
        Vector3 targetPosition = player.position + (Vector3)offset;
        Vector3 desiredPosition = currentPosition;
        desiredPosition.z = transform.position.z;

        float deltaX = targetPosition.x - currentPosition.x;
        if (Mathf.Abs(deltaX) > splitDeadZone.x)
            desiredPosition.x += deltaX - Mathf.Sign(deltaX) * splitDeadZone.x;

        float deltaY = targetPosition.y - currentPosition.y;
        if (Mathf.Abs(deltaY) > splitDeadZone.y)
            desiredPosition.y += deltaY - Mathf.Sign(deltaY) * splitDeadZone.y;

        return desiredPosition;
    }

    void MoveCameraSmooth(Transform cameraTransform, Camera cameraComponent, Vector3 targetPosition, float targetZoom, float smoothness)
    {
        Vector3 finalPosition = targetPosition + shakeOffset;
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, finalPosition, smoothness * Time.deltaTime);
        cameraTransform.position = ClampToMap(cameraTransform.position, cameraComponent);
        UpdateCameraZoom(cameraComponent, targetZoom);
    }

    void SetCameraPose(Camera cameraComponent, Vector3 targetPosition, float targetZoom)
    {
        if (cameraComponent == null) return;
        cameraComponent.transform.rotation = transform.rotation;
        cameraComponent.transform.position = ClampToMap(targetPosition + shakeOffset, cameraComponent);
        if (cameraComponent.orthographic)
            cameraComponent.orthographicSize = ClampZoomToMap(targetZoom, cameraComponent);
    }

    void UpdateCameraZoom(Camera cameraComponent, float targetZoom)
    {
        if (cameraComponent == null || !cameraComponent.orthographic) return;
        targetZoom = ClampZoomToMap(targetZoom, cameraComponent);
        cameraComponent.orthographicSize = Mathf.Lerp(cameraComponent.orthographicSize, targetZoom, Time.deltaTime * zoomSmoothness);
    }

    float GetSplitZoom(Camera cameraComponent) => ClampZoomToMap(Mathf.Max(0.1f, splitZoom), cameraComponent);

    float ClampZoomToMap(float targetZoom, Camera cameraComponent)
    {
        if (mapSprite == null || cameraComponent == null || !cameraComponent.orthographic)
            return targetZoom;

        Bounds mapBounds = mapSprite.bounds;
        float aspect = Mathf.Max(0.01f, cameraComponent.aspect);
        float maxVerticalZoom = mapBounds.size.y * 0.5f;
        float maxHorizontalZoom = (mapBounds.size.x * 0.5f) / aspect;
        return Mathf.Min(targetZoom, Mathf.Min(maxVerticalZoom, maxHorizontalZoom));
    }

    Vector3 ClampToMap(Vector3 position, Camera cameraComponent)
    {
        if (mapSprite == null || cameraComponent == null || !cameraComponent.orthographic)
            return position;

        Bounds mapBounds = mapSprite.bounds;
        float camHeight = cameraComponent.orthographicSize;
        float camWidth = camHeight * cameraComponent.aspect;

        float minX = mapBounds.min.x + camWidth;
        float maxX = mapBounds.max.x - camWidth;
        float minY = mapBounds.min.y + camHeight;
        float maxY = mapBounds.max.y - camHeight;

        Vector3 clampedPosition = position;
        clampedPosition.x = minX > maxX ? mapBounds.center.x : Mathf.Clamp(clampedPosition.x, minX, maxX);
        clampedPosition.y = minY > maxY ? mapBounds.center.y : Mathf.Clamp(clampedPosition.y, minY, maxY);
        return clampedPosition;
    }

    void InitializeSplitPositionsFromSharedCrop()
    {
        Vector3 sharedPosition = sharedCamera != null ? sharedCamera.transform.position : transform.position;
        float sharedZoom = sharedCamera != null ? sharedCamera.orthographicSize : minZoom;

        leftSplitPosition = GetSharedCropPosition(sharedPosition, sharedZoom, true);
        rightSplitPosition = GetSharedCropPosition(sharedPosition, sharedZoom, false);

        SetCameraPose(leftSplitCamera, leftSplitPosition, sharedZoom);
        SetCameraPose(rightSplitCamera, rightSplitPosition, sharedZoom);

        splitPositionsInitialized = true;
    }

    void ForceSharedLayout()
    {
        if (splitTransitionRoutine != null)
        {
            StopCoroutine(splitTransitionRoutine);
            splitTransitionRoutine = null;
        }

        splitState = SplitState.Shared;
        splitProgress = 0f;
        splitPositionsInitialized = false;
        ApplyCameraLayout();
        UpdateDivider();

        if (leftSplitCamera != null) leftSplitCamera.enabled = false;
        if (rightSplitCamera != null) rightSplitCamera.enabled = false;
    }

    void UpdateShake()
    {
        float shakeScale = GetShakeScale();
        if (shakeScale <= 0f)
        {
            shakeOffset = Vector3.Lerp(shakeOffset, Vector3.zero, Time.deltaTime * shakeDamping);
            shakeIntensity = 0f;
            shakeDuration = 0f;
            return;
        }

        if (shakeDuration > 0f)
        {
            shakeDuration -= Time.deltaTime;
            float effectiveIntensity = Mathf.Min(shakeIntensity * shakeScale, maxShakeOffset);
            shakeOffset = new Vector3(
                (Random.value - 0.5f) * 2f,
                (Random.value - 0.5f) * 2f,
                0f) * effectiveIntensity;
        }
        else
        {
            shakeOffset = Vector3.Lerp(shakeOffset, Vector3.zero, Time.deltaTime * shakeDamping);
            shakeIntensity = 0f;
        }
    }

    float GetShakeScale()
    {
        if (!cameraShakeEnabled) return 0f;

        // �� ����� ���������� ������ ��������� shake �� ������
        if (isCinematic) return 1f;

        if (IsSplitScreenActive && disableShakeDuringSplitScreen) return 0f;

        float levelScale = Mathf.Clamp(shakeIntensityLevel, 1f, 10f) / 10f;
        if (IsSplitScreenActive)
            levelScale *= Mathf.Clamp01(splitScreenShakeMultiplier);

        return levelScale;
    }

    void OnDisable()
    {
        if (sharedCamera != null)
        {
            sharedCamera.enabled = true;
            sharedCamera.rect = new Rect(0f, 0f, 1f, 1f);
            sharedCamera.cullingMask = originalSharedCullingMask;
            sharedCamera.clearFlags = originalSharedClearFlags;
        }

        if (leftSplitCamera != null) leftSplitCamera.enabled = false;
        if (rightSplitCamera != null) rightSplitCamera.enabled = false;
        if (dividerImage != null) dividerImage.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (createdLeftSplitCamera && leftSplitCamera != null) Destroy(leftSplitCamera.gameObject);
        if (createdRightSplitCamera && rightSplitCamera != null) Destroy(rightSplitCamera.gameObject);
        if (createdDividerCanvas && dividerCanvas != null) Destroy(dividerCanvas.gameObject);
    }

    void OnValidate()
    {
        minZoom = Mathf.Max(0.1f, minZoom);
        maxZoom = Mathf.Max(minZoom, maxZoom);
        splitZoom = Mathf.Max(0.1f, splitZoom);
        splitThreshold = Mathf.Max(0.1f, splitThreshold);
        mergeThreshold = Mathf.Clamp(mergeThreshold, 0.1f, splitThreshold);
        splitDuration = Mathf.Max(0.01f, splitDuration);
        splitDeadZone.x = Mathf.Max(0f, splitDeadZone.x);
        splitDeadZone.y = Mathf.Max(0f, splitDeadZone.y);
        dividerLineWidth = Mathf.Max(0f, dividerLineWidth);
        dividerMaxWidth = Mathf.Max(dividerLineWidth, dividerMaxWidth);
        shakeIntensityLevel = Mathf.Clamp(shakeIntensityLevel, 1f, 10f);
        splitScreenShakeMultiplier = Mathf.Clamp01(splitScreenShakeMultiplier);
        maxShakeOffset = Mathf.Max(0f, maxShakeOffset);
        shakeDamping = Mathf.Max(0f, shakeDamping);

        if (splitEase == null || splitEase.length == 0)
            splitEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    public static void Shake(float intensity, float duration)
    {
        shakeIntensity = Mathf.Max(shakeIntensity, intensity);
        shakeDuration = Mathf.Max(shakeDuration, duration);
    }

    public static void SetCinematicTarget(Vector3 target, float zoom)
    {
        if (instance == null) return;
        instance.isCinematic = true;
        instance.cinematicTarget = target;
        instance.cinematicZoom = zoom;
    }

    public static void RestoreNormal()
    {
        if (instance == null) return;
        instance.isCinematic = false;
    }
}