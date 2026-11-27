using System;

using UnityEngine;

using UnityEngine.EventSystems;

using UnityEngine.UI;



public class SemicircleClockSlider : MonoBehaviour, IPointerDownHandler, IDragHandler

{

public event Action<float> HourChanged;



RectTransform root;

RectTransform trackRect;

RectTransform arcPivot;

RectTransform handle;

Image handleImage;

Text timeLabel;

float hour = 12f;

const float InnerRatio = 0.78f;

const float OuterRatio = 0.988f;



public static SemicircleClockSlider Create()

{

GameObject canvasGo = new GameObject("DayNightCanvas");

Canvas canvas = canvasGo.AddComponent<Canvas>();

canvas.renderMode = RenderMode.ScreenSpaceOverlay;

canvas.sortingOrder = 100;

CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();

scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

scaler.referenceResolution = new Vector2(1920f, 1080f);

scaler.matchWidthOrHeight = 0.5f;

canvasGo.AddComponent<GraphicRaycaster>();



GameObject widget = CreateObject("ClockWidget", canvasGo.transform);

SemicircleClockSlider slider = widget.AddComponent<SemicircleClockSlider>();

slider.Build(widget.GetComponent<RectTransform>());

return slider;

}



void Build(RectTransform widget)

{

root = widget;

root.anchorMin = new Vector2(0f, 1f);

root.anchorMax = new Vector2(0f, 1f);

root.pivot = new Vector2(0f, 1f);

root.anchoredPosition = new Vector2(24f, -20f);

root.sizeDelta = new Vector2(340f, 250f);



Image panel = widget.gameObject.AddComponent<Image>();

panel.sprite = UiSpriteFactory.RoundRect(256, 192, 22, Color.white);

panel.type = Image.Type.Sliced;

panel.color = new Color(0.16f, 0.16f, 0.17f, 0.82f);



GameObject arcHostGo = CreateObject("ArcHost", root);

RectTransform arcHost = arcHostGo.GetComponent<RectTransform>();

arcHost.anchorMin = new Vector2(0.5f, 1f);

arcHost.anchorMax = new Vector2(0.5f, 1f);

arcHost.pivot = new Vector2(0.5f, 1f);

arcHost.anchoredPosition = new Vector2(0f, -8f);

arcHost.sizeDelta = new Vector2(220f, 220f);



Image track = AddImage("Track", arcHost, UiSpriteFactory.SplitNightDayArc(256, InnerRatio));

trackRect = track.rectTransform;

Stretch(trackRect, Vector2.zero, Vector2.one);

track.preserveAspect = true;

track.raycastTarget = true;



Image moon = AddImage("Moon", arcHost, UiSpriteFactory.Moon(64));

Place(moon.rectTransform, new Vector2(0.08f, 0.5f), 26f);

moon.color = new Color(0.78f, 0.82f, 0.9f, 0.9f);



Image sun = AddImage("Sun", arcHost, UiSpriteFactory.Sun(96));

Place(sun.rectTransform, new Vector2(0.92f, 0.5f), 28f);

sun.color = new Color(1f, 0.9f, 0.7f, 0.95f);



GameObject pivotGo = CreateObject("ArcPivot", arcHost);

arcPivot = pivotGo.GetComponent<RectTransform>();

arcPivot.anchorMin = new Vector2(0.5f, 0.5f);

arcPivot.anchorMax = new Vector2(0.5f, 0.5f);

arcPivot.pivot = new Vector2(0.5f, 0.5f);

arcPivot.anchoredPosition = Vector2.zero;

arcPivot.sizeDelta = Vector2.zero;



handleImage = AddImage("Handle", arcPivot, UiSpriteFactory.Disc(64, Color.white));

handle = handleImage.rectTransform;

handle.sizeDelta = new Vector2(16f, 16f);



timeLabel = AddText("TimeLabel", arcHost, 18);

Stretch(timeLabel.rectTransform, new Vector2(0.2f, 0.52f), new Vector2(0.8f, 0.72f));

timeLabel.color = new Color(0.92f, 0.92f, 0.93f, 0.95f);



Text nightCaption = AddText("NightCaption", arcHost, 11);

Place(nightCaption.rectTransform, new Vector2(0.22f, 0.38f), 70f, 18f);

nightCaption.text = "Gece";

nightCaption.color = new Color(0.7f, 0.74f, 0.8f, 0.8f);



Text dayCaption = AddText("DayCaption", arcHost, 11);

Place(dayCaption.rectTransform, new Vector2(0.78f, 0.38f), 70f, 18f);

dayCaption.text = "Gündüz";

dayCaption.color = new Color(0.86f, 0.8f, 0.66f, 0.85f);



CreatePresetButton("Gün doğumu", 6.1f, 0f);

CreatePresetButton("Öğlen", 16.5f, 1f);

CreatePresetButton("Gün batımı", 17.7f, 2f);

CreatePresetButton("Gece", 0f, 3f);

}



void CreatePresetButton(string label, float presetHour, float index)

{

GameObject go = CreateObject(label, root);

RectTransform rect = go.GetComponent<RectTransform>();

rect.anchorMin = new Vector2(0.06f + index * 0.235f, 0.06f);

rect.anchorMax = new Vector2(0.06f + index * 0.235f + 0.215f, 0.22f);

rect.offsetMin = Vector2.zero;

rect.offsetMax = Vector2.zero;



Image image = go.AddComponent<Image>();

image.sprite = UiSpriteFactory.RoundRect(64, 32, 10, Color.white);

image.type = Image.Type.Sliced;

image.color = new Color(0.24f, 0.24f, 0.25f, 0.95f);

image.raycastTarget = true;



Button button = go.AddComponent<Button>();

ColorBlock colors = button.colors;

colors.normalColor = Color.white;

colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);

colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);

button.colors = colors;

button.targetGraphic = image;

float captured = presetHour;

button.onClick.AddListener(() => HourChanged?.Invoke(captured));



Text text = AddText(label + "Label", go.transform, 10);

Stretch(text.rectTransform, Vector2.zero, Vector2.one);

text.text = label;

text.color = new Color(0.88f, 0.88f, 0.88f, 1f);

text.fontSize = 11;

}



public void SetHour(float value, bool isNight)

{

hour = Mathf.Repeat(value, 24f);

RefreshVisuals();

}



public void OnPointerDown(PointerEventData eventData) => SetFromPointer(eventData);



public void OnDrag(PointerEventData eventData) => SetFromPointer(eventData);



void SetFromPointer(PointerEventData eventData)

{

if (arcPivot == null)

return;

if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(arcPivot, eventData.position, eventData.pressEventCamera, out Vector2 local))

return;

if (local.y < 0f)

local.y = 0f;



float angle = Mathf.Atan2(local.y, local.x);

angle = Mathf.Clamp(angle, 0f, Mathf.PI);

HourChanged?.Invoke(AngleToHour(angle));

}



float RingRadius()

{

if (trackRect == null)

return 88f;

float size = Mathf.Min(trackRect.rect.width, trackRect.rect.height);

return size * 0.5f * (InnerRatio + OuterRatio) * 0.5f;

}



public static float AngleToHour(float angle)

{

if (angle >= Mathf.PI * 0.5f)

{

float nightT = (Mathf.PI - angle) / (Mathf.PI * 0.5f);

float hour = 18f + nightT * 12f;

return hour >= 24f ? hour - 24f : hour;

}



float dayT = (Mathf.PI * 0.5f - angle) / (Mathf.PI * 0.5f);

return 6f + dayT * 12f;

}



public static float HourToAngle(float hour)

{

hour = Mathf.Repeat(hour, 24f);

if (hour > 18f || hour < 6f)

{

float nightT = hour > 18f ? (hour - 18f) / 12f : (hour + 6f) / 12f;

return Mathf.Lerp(Mathf.PI, Mathf.PI * 0.5f, nightT);

}



return Mathf.Lerp(Mathf.PI * 0.5f, 0f, (hour - 6f) / 12f);

}



void RefreshVisuals()

{

if (handle == null)

return;



bool nightSide = hour > 18f || hour < 6f;

handleImage.color = nightSide

? new Color(0.82f, 0.86f, 0.94f, 1f)

: new Color(1f, 0.93f, 0.78f, 1f);



float angle = HourToAngle(hour);

handle.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * RingRadius();



int h = Mathf.FloorToInt(hour);

int m = Mathf.FloorToInt((hour - h) * 60f);

timeLabel.text = $"{h:00}:{m:00}";

}



static Image AddImage(string name, Transform parent, Sprite sprite)

{

GameObject go = CreateObject(name, parent);

Image image = go.AddComponent<Image>();

image.sprite = sprite;

image.raycastTarget = false;

return image;

}



static Text AddText(string name, Transform parent, int size)

{

GameObject go = CreateObject(name, parent);

Text text = go.AddComponent<Text>();

text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

text.fontSize = size;

text.alignment = TextAnchor.MiddleCenter;

text.color = Color.white;

text.raycastTarget = false;

text.horizontalOverflow = HorizontalWrapMode.Overflow;

text.verticalOverflow = VerticalWrapMode.Overflow;

return text;

}



static GameObject CreateObject(string name, Transform parent)

{

GameObject go = new GameObject(name, typeof(RectTransform));

go.transform.SetParent(parent, false);

return go;

}



static void Stretch(RectTransform rect, Vector2 min, Vector2 max)

{

rect.anchorMin = min;

rect.anchorMax = max;

rect.offsetMin = Vector2.zero;

rect.offsetMax = Vector2.zero;

}



static void Place(RectTransform rect, Vector2 anchor, float size)

{

Place(rect, anchor, size, size);

}



static void Place(RectTransform rect, Vector2 anchor, float width, float height)

{

rect.anchorMin = anchor;

rect.anchorMax = anchor;

rect.pivot = new Vector2(0.5f, 0.5f);

rect.anchoredPosition = Vector2.zero;

rect.sizeDelta = new Vector2(width, height);

}

}