using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI_Toolkit.model;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.UIElements;
using ZXing;
using Button = UnityEngine.UIElements.Button;

namespace UI_Toolkit.controller
{
    public class ButtonClick : MonoBehaviour
    {
        private UIDocument _root;
        private Button _scan;
        private Dictionary<Button, Action> _buttonActionDictionary;
        public VisualTreeAsset projectAsset;
        private VisualElement _projectElement;
        private string create_by = "IAFahim";

        private string apiKey;
        private string authorization;
        
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RawImage rawImageBackground;
        [SerializeField]private AspectRatioFitter aspectRatioFitter;
        [SerializeField] private TextMeshProUGUI textOut;
        [SerializeField] private RectTransform scanZone;

        private bool _isCamAvaible;
        private WebCamTexture _cameraTexture;

        private void Start()
        {
            _projectElement = projectAsset.Instantiate();
            apiKey =
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im9lYWllam9jYmh3c2JxbXdlcWh4Iiwicm9sZSI6ImFub24iLCJpYXQiOjE2NjU3Nzk2NzAsImV4cCI6MTk4MTM1NTY3MH0.PUd7UednT37wcvgf2Iqr5EJX1rDuW1Q2Nw3ACCnOfNI";
            authorization = "Bearer " + apiKey;
            _root = GetComponent<UIDocument>();
            _buttonActionDictionary = new Dictionary<Button, Action>()
            {
                { _root.rootVisualElement.Q<Button>("Project"), Project },
                { _root.rootVisualElement.Q<Button>("Group"), Group },
                { _root.rootVisualElement.Q<Button>("Quiz"), Quiz },
                { _root.rootVisualElement.Q<Button>("Scan"), Scan },
            };
            foreach (var button in _buttonActionDictionary.Keys)
            {
                button.clickable.clicked += _buttonActionDictionary[button];
            }
            SetUpCamera();
        }

        private void Update()
        {
            UpdateCameraRender();
        }

        private void UpdateCameraRender()
        {
            if (_isCamAvaible == false)
            {
                return;
            }

            float ratio = (float)_cameraTexture.width / (float)_cameraTexture.height;
            aspectRatioFitter.aspectRatio = ratio;

            int orientation = _cameraTexture.videoRotationAngle;
            orientation = orientation * 3;
            rawImageBackground.rectTransform.localEulerAngles = new Vector3(0, 0, orientation);
        }

        private void OnDisable()
        {
            foreach (var button in _buttonActionDictionary.Keys)
            {
                button.clickable.clicked -= _buttonActionDictionary[button];
            }
        }

        private void Project()
        {
            Debug.Log("Project Clicked");
            StartCoroutine(MakeRequest("projects"));
        }

        IEnumerator MakeRequest(string urlEnd)
        {
            using UnityWebRequest request =
                UnityWebRequest.Get("https://oeaiejocbhwsbqmweqhx.supabase.co/rest/v1/" + urlEnd +"?select=*&created_by=eq."+ create_by);
            request.SetRequestHeader("apikey", apiKey);
            request.SetRequestHeader("Authorization", authorization);
            yield return request.SendWebRequest();
            switch (request.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ProtocolError:
                    Debug.Log(request.error);
                    break;
                case UnityWebRequest.Result.Success:
                    Debug.Log(request.downloadHandler.text);
                    Projects data =
                        JsonUtility.FromJson<Projects>("{\"projects\":" + request.downloadHandler.text + "}");
                    AddProjectToScrollView(data.projects);
                    break;
            }
        }

        private void AddProjectToScrollView(Project[] data)
        {
            var scrollView = _root.rootVisualElement.Q<ScrollView>("ScrollView");
            foreach (var project in data)
            {
                var projectElement = projectAsset.Instantiate();
                projectElement.Q<Label>("name").text = project.name;
                projectElement.Q<Label>("type").text = project.type;
                projectElement.Q<Label>("description").text = project.description;
                scrollView.Add(projectElement);
            }
        }

        private void Group()
        {
            Debug.Log("Group Clicked");
        }

        private void Quiz()
        {
            Debug.Log("Quiz Clicked");
        }

        private async void Scan()
        {
            try
            {
                IBarcodeReader barcodeReader = new BarcodeReader();
                Result result = barcodeReader.Decode(_cameraTexture.GetPixels32(), _cameraTexture.width,
                    _cameraTexture.height);
                if (result != null)
                {
                    textOut.text = result.Text;
                    create_by = result.Text;
                    DisableCam();
                }
                else
                {
                    textOut.text = "Failed to Read QR CODE";
                }
            }
            catch
            {
                textOut.text = "FAILED IN TRY";
            }

            Debug.Log("Scan Clicked");
        }
        
        private void SetUpCamera()
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            
            if (devices.Length == 0)
            {
                textOut.text="No Camera Detected";
                _isCamAvaible = false;
                return;
            }
            
            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i].isFrontFacing == false)
                {
                    var rect = scanZone.rect;
                    _cameraTexture = new WebCamTexture(devices[i].name, (int)rect.width,
                        (int)rect.height);
                    break;
                }
            }
            
            _isCamAvaible = true;
            _cameraTexture.Play();
            rawImageBackground.texture = _cameraTexture;
        }

        private void DisableCam()
        {
            _isCamAvaible = false;
            _cameraTexture.Stop();
            rawImageBackground.texture = null;
            _canvas.gameObject.SetActive(false);
        }
    }
    
}