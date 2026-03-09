"""
DaySim Web Companion
Reads and writes the same daysim_save.json that the Unity game uses,
giving you a browser dashboard to log actions and view stats.
"""

import json
import os
import re
from datetime import datetime, timezone, timedelta
from flask import Flask, jsonify, render_template, request

app = Flask(__name__)

# ── Save file location ────────────────────────────────────────────────────────
# Mirror of Unity's Application.persistentDataPath on Windows.
UNITY_SAVE_PATH = os.path.join(
    os.environ.get("USERPROFILE", os.path.expanduser("~")),
    "AppData", "LocalLow", "DefaultCompany", "DaySim", "daysim_save.json"
)
# Fallback: a local save file next to app.py for standalone use.
LOCAL_SAVE_PATH = os.path.join(os.path.dirname(__file__), "daysim_save.json")


def get_save_path():
    if os.path.exists(UNITY_SAVE_PATH):
        return UNITY_SAVE_PATH
    return LOCAL_SAVE_PATH


def load_save():
    path = get_save_path()
    if not os.path.exists(path):
        return default_save()
    try:
        with open(path, "r", encoding="utf-8") as f:
            return json.load(f)
    except Exception:
        return default_save()


def write_save(data):
    path = get_save_path()
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)


def default_save():
    return {
        "AvatarStats": {"Level": 1, "CurrentXp": 0.0},
        "NeedsState": {"Hunger": 70, "Energy": 70, "Hygiene": 70, "Fun": 70, "Social": 70},
        "Actions": [],
        "Streaks": [],
        "QuestStates": [],
        "Achievements": [],
        "LastSavedUtc": datetime.now(timezone.utc).isoformat()
    }


# ── NLP parser (mirrors UserAction.cs logic) ─────────────────────────────────

ACTION_MAP = {
    "WakeUp":       ["wake", "woke", "got up", "get up", "rise", "arose", "morning", "out of bed"],
    "Sleep":        ["sleep", "slept", "went to bed", "bedtime", "good night", "nap", "napped"],
    "BrushTeeth":   ["brush", "teeth", "floss", "mouthwash", "shower", "showered", "bathe",
                     "bathed", "bath", "shampoo", "wash hair", "groomed"],
    "EatBreakfast": ["breakfast", "break fast", "morning meal"],
    "EatLunch":     ["lunch", "midday meal", "lunchtime"],
    "EatDinner":    ["dinner", "supper", "evening meal"],
    "DrinkWater":   ["water", "drink", "drank", "hydrat", "juice", "tea", "coffee", "smoothie"],
    "Exercise":     ["exercise", "workout", "gym", "jog", "jogged", "run", "ran", "walk", "walked",
                     "hike", "swim", "swam", "yoga", "pilates", "lift", "weights", "bike", "biked",
                     "cycling", "squat", "stretch", "cardio", "training", "trained", "sport",
                     "tennis", "football", "soccer", "basketball", "climb"],
    "Study":        ["study", "studied", "learn", "learned", "class", "lecture", "homework",
                     "assignment", "revision", "revise", "research", "course"],
    "Work":         ["work", "worked", "job", "office", "meeting", "call", "email", "project",
                     "task", "coding", "code", "coded", "programming", "presentation", "report",
                     "deadline", "client"],
    "Relax":        ["relax", "relaxed", "chill", "chilled", "rest", "rested", "meditate",
                     "meditation", "game", "gaming", "gamed", "watch", "movie", "tv", "netflix",
                     "youtube", "music", "podcast", "hobby", "hang out", "hangout", "friends",
                     "social", "party", "chat"],
}

CATEGORY_MAP = {
    "WakeUp": "Sleep", "Sleep": "Sleep",
    "BrushTeeth": "Hygiene",
    "EatBreakfast": "Nutrition", "EatLunch": "Nutrition", "EatDinner": "Nutrition",
    "DrinkWater": "Hydration",
    "Exercise": "Exercise",
    "Study": "WorkStudy", "Work": "WorkStudy",
    "Relax": "Relaxation",
}

GENERIC_EAT = ["eat", "ate", "meal", "food", "snack", "cooked", "cook", "sandwich",
               "salad", "pizza", "burger", "pasta", "rice", "soup"]

DEFAULT_DURATIONS = {
    "Sleep": 480, "WakeUp": 5, "Exercise": 30, "Study": 60, "Work": 60,
    "Relax": 30, "EatBreakfast": 20, "EatLunch": 20, "EatDinner": 20,
    "BrushTeeth": 5, "DrinkWater": 2,
}

ACTION_TYPE_IDS = {
    "Unknown": 0, "WakeUp": 10, "BrushTeeth": 20, "EatBreakfast": 30,
    "EatLunch": 31, "EatDinner": 32, "DrinkWater": 40, "Exercise": 50,
    "Study": 60, "Work": 70, "Relax": 80, "Sleep": 90,
}


def parse_action(text):
    n = text.strip().lower()

    for action_type, keywords in ACTION_MAP.items():
        if any(kw in n for kw in keywords):
            return action_type

    if any(kw in n for kw in GENERIC_EAT):
        hour = datetime.now().hour
        if hour < 11:   return "EatBreakfast"
        if hour < 16:   return "EatLunch"
        return "EatDinner"

    return None


def extract_duration(text, default):
    words = re.split(r'[\s,;]+', text.lower())
    for i, word in enumerate(words[:-1]):
        try:
            val = float(word)
        except ValueError:
            continue
        unit = words[i + 1].rstrip('.,;')
        if unit in ("hour", "hours", "hr", "hrs", "h"):
            return val * 60
        if unit in ("minute", "minutes", "min", "mins", "m"):
            return val

    # Handle "30-minute" style
    m = re.search(r'(\d+(?:\.\d+)?)[- ](min|minute|minutes|hour|hours|hr)', text, re.I)
    if m:
        val = float(m.group(1))
        return val * 60 if 'h' in m.group(2).lower() else val

    return default


# ── XP + needs helpers ────────────────────────────────────────────────────────

XP_REWARDS = {
    "WakeUp": 5, "BrushTeeth": 10, "EatBreakfast": 8, "EatLunch": 8,
    "EatDinner": 8, "DrinkWater": 3, "Exercise": 20, "Study": 12,
    "Work": 12, "Relax": 4, "Sleep": 10,
}

NEEDS_DELTAS = {
    "EatBreakfast": {"Hunger": 25, "Energy": 5,  "Social": 8},
    "EatLunch":     {"Hunger": 25, "Energy": 5,  "Social": 8},
    "EatDinner":    {"Hunger": 25, "Energy": 5,  "Social": 8},
    "DrinkWater":   {"Hunger": 5},
    "Sleep":        {"Energy": 40, "Hygiene": -5},
    "WakeUp":       {"Energy": 10},
    "BrushTeeth":   {"Hygiene": 20, "Fun": 2},
    "Exercise":     {"Energy": -10, "Fun": 15, "Hygiene": -10, "Social": 5},
    "Relax":        {"Fun": 20,  "Energy": 5, "Social": 10},
    "Study":        {"Fun": -5,  "Energy": -5, "Social": -3},
    "Work":         {"Fun": -5,  "Energy": -5, "Social": -3},
}


def compute_mood(needs):
    h, e = needs.get("Hunger", 70), needs.get("Energy", 70)
    score = (h * 2 + e * 2 + needs.get("Hygiene", 70) +
             needs.get("Fun", 70) + needs.get("Social", 70)) / 7.0
    critical = h < 20 or e < 20
    if critical:
        score = min(score, 44)
    if score < 25:  return "VeryBad"
    if score < 45:  return "Bad"
    if score < 65:  return "Neutral"
    if score < 85:  return "Good"
    return "Great"


def xp_for_level(level, base=100, factor=1.2):
    return base * (factor ** (level - 1))


def apply_xp(stats, xp):
    stats["CurrentXp"] = stats.get("CurrentXp", 0) + xp
    level = stats.get("Level", 1)
    while stats["CurrentXp"] >= xp_for_level(level):
        stats["CurrentXp"] -= xp_for_level(level)
        level += 1
    stats["Level"] = level


def clamp(val, lo=0, hi=100):
    return max(lo, min(hi, val))


# ── Analytics helpers ─────────────────────────────────────────────────────────

def actions_last_n_days(actions, n):
    cutoff = datetime.now(timezone.utc) - timedelta(days=n)
    result = []
    for a in actions:
        try:
            ts = datetime.fromisoformat(a.get("TimestampUtc", "").replace("Z", "+00:00"))
            if ts >= cutoff:
                result.append(a)
        except Exception:
            pass
    return result


def category_counts(actions):
    counts = {}
    for a in actions:
        cat = a.get("Category", "Unknown")
        if cat and cat != "Unknown":
            counts[cat] = counts.get(cat, 0) + 1
    return counts


# ── Routes ────────────────────────────────────────────────────────────────────

@app.route("/")
def index():
    return render_template("index.html")


@app.route("/api/state")
def api_state():
    data = load_save()
    needs = data.get("NeedsState") or {}
    stats = data.get("AvatarStats") or {}
    actions = data.get("Actions") or []
    streaks = data.get("Streaks") or []
    achievements = data.get("Achievements") or []

    # Annotate actions with human-readable type name
    id_to_name = {v: k for k, v in ACTION_TYPE_IDS.items()}
    enriched_actions = []
    for a in reversed(actions[-50:]):  # most recent 50, newest first
        type_name = id_to_name.get(a.get("ActionType", 0), "Unknown")
        enriched_actions.append({
            "type": type_name,
            "category": CATEGORY_MAP.get(type_name, "Unknown"),
            "timestamp": a.get("TimestampUtc", ""),
            "duration": a.get("EstimatedDurationMinutes", 0),
            "raw": a.get("RawText", ""),
        })

    week_actions = actions_last_n_days(
        [{"Category": CATEGORY_MAP.get(id_to_name.get(a.get("ActionType",0),"Unknown"),"Unknown"),
          "TimestampUtc": a.get("TimestampUtc","")} for a in actions], 7)
    week_counts = category_counts(week_actions)

    level = stats.get("Level", 1)
    current_xp = stats.get("CurrentXp", 0)
    xp_needed = xp_for_level(level)
    xp_pct = round(current_xp / xp_needed * 100) if xp_needed else 0

    return jsonify({
        "stats": {
            "level": level,
            "currentXp": round(current_xp, 1),
            "xpNeeded": round(xp_needed, 1),
            "xpPercent": xp_pct,
        },
        "needs": {k: round(clamp(v), 1) for k, v in needs.items()},
        "mood": compute_mood(needs),
        "actions": enriched_actions,
        "streaks": streaks,
        "achievements": achievements,
        "weekCounts": week_counts,
        "saveSource": "Unity save" if os.path.exists(UNITY_SAVE_PATH) else "local",
        "lastSaved": data.get("LastSavedUtc", ""),
    })


@app.route("/api/log", methods=["POST"])
def api_log():
    body = request.get_json(silent=True) or {}
    text = (body.get("text") or "").strip()
    if not text:
        return jsonify({"error": "No text provided"}), 400

    action_type = parse_action(text)
    if not action_type:
        return jsonify({"error": "unrecognized", "hint": "Try: jogged 30 min, ate lunch, brushed teeth…"}), 422

    duration = extract_duration(text, DEFAULT_DURATIONS.get(action_type, 10))
    now_utc = datetime.now(timezone.utc).isoformat()

    data = load_save()
    if data.get("Actions") is None:
        data["Actions"] = []
    if data.get("NeedsState") is None:
        data["NeedsState"] = {"Hunger": 70, "Energy": 70, "Hygiene": 70, "Fun": 70, "Social": 70}
    if data.get("AvatarStats") is None:
        data["AvatarStats"] = {"Level": 1, "CurrentXp": 0.0}

    data["Actions"].append({
        "ActionType": ACTION_TYPE_IDS.get(action_type, 0),
        "TimestampUtc": now_utc,
        "EstimatedDurationMinutes": duration,
        "RawText": text,
    })

    # Apply needs deltas
    needs = data["NeedsState"]
    for key, delta in NEEDS_DELTAS.get(action_type, {}).items():
        needs[key] = clamp(needs.get(key, 70) + delta)

    # Award XP
    apply_xp(data["AvatarStats"], XP_REWARDS.get(action_type, 1))

    data["LastSavedUtc"] = now_utc
    write_save(data)

    return jsonify({
        "logged": action_type,
        "category": CATEGORY_MAP.get(action_type, "Unknown"),
        "duration": duration,
        "xpAwarded": XP_REWARDS.get(action_type, 1),
        "level": data["AvatarStats"]["Level"],
        "mood": compute_mood(needs),
    })


if __name__ == "__main__":
    app.run(debug=True, port=5000)
