using System.Collections.Generic;

namespace EverySceneUtil {
    internal class TransitionData {
        public static List<TransitionData> allTransitions = new();

        public string scene = "";
        public string gate = "";
        public bool infection = false;
        public BoolTest[] boolTests = [];
        public IntTest[] intTests = [];
        public bool hasAlt = false;
        public bool isDream = false;
    }

    internal class BoolTest {
        public string name = "";
        public bool value = false;
    }

    internal class IntTest {
        public string name = "";
        public int value = 0;
    }
}
