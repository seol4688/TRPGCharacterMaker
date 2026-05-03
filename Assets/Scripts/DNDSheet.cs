using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Michsky.UI.Dark;
using System;

namespace DND
{
    using static Define;

    public class DNDSheet : MonoBehaviour
    {
        //public GameObject _selectSources;
        public Races _selectRace;

        [Serializable]
        public struct Source
        {
            public bool _PHB;
            public bool _EEP;
            public bool _SAG;
            public bool _VGM;
            public bool _XGE;
            public bool _TCE;
            public bool _SCC;
        }
        public Source _sources;

        [Serializable]
        public struct Race
        {
            public GameObject _dragonborn;
            public GameObject _dwarf;
            public GameObject _elf;
            public GameObject _gnome;
            public GameObject _halfelf;
            public GameObject _halfling;
            public GameObject _halforc;
            public GameObject _human;
            public GameObject _tiefling;
        }
        public Race _races;



        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            SetRace();
        }

        #region SourceBtn
        public void SetPHB(bool value)
        {
            _sources._PHB = value;
        }

        public void SetEEP(bool value)
        {
            _sources._EEP = value;
        }

        public void SetSAG(bool value)
        {
            _sources._SAG = value;
        }

        public void SetVGM(bool value)
        {
            _sources._VGM = value;
        }

        public void SetXGE(bool value)
        {
            _sources._XGE = value;
        }

        public void SetTCE(bool value)
        {
            _sources._TCE = value;
        }

        public void SetSCC(bool value)
        {
            _sources._SCC = value;
        }
        #endregion

        void SetRace() //활성화 소스에 따른 종족활성화
        {
            if (_sources._PHB || _sources._EEP)
                _races._gnome.SetActive(true);
            else
                _races._gnome.SetActive(false);

            if (_sources._PHB || _sources._SAG)
            {
                _races._dwarf.SetActive(true);
                _races._halfelf.SetActive(true);
                _races._halfling.SetActive(true);
                _races._tiefling.SetActive(true);
            }
            else
            {
                _races._dwarf.SetActive(false);
                _races._halfelf.SetActive(false);
                _races._halfling.SetActive(false);
                _races._tiefling.SetActive(false);
            }

            _races._dragonborn.SetActive(_sources._PHB);
            _races._elf.SetActive(_sources._PHB);
            _races._halforc.SetActive(_sources._PHB);
            _races._human.SetActive(_sources._PHB);
        }


        #region RaceBtn
        public void SetRace(int num)
        {
            _selectRace = (Races)num;
        }
        #endregion
    }
}
