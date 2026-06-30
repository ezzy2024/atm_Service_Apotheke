// ==========================================
// aTM Kiosk Terminal - Parametric Enclosure
// Target Printer: Bambu Lab (256x256mm Bed)
// Target Device: iPad 10th Gen (10.9")
// eGK Reader: CHERRY TC 1200
// ==========================================

// --- Rendering Resolution ---
$fn = 60;

// --- Parametrische Einstellungen ---
// 1. Tablet (iPad 10)
tablet_w = 248.6;
tablet_h = 179.5;
tablet_d = 7.0;

// Toleranz für den Einschub (Passung)
tol = 0.8;

// Display Ausschnitt
display_w = 236;
display_h = 164;

// Wandstärken
wall_th = 4.0;
face_th = 3.0; // Dicke der Frontblende

// 2. eGK-Lesegerät (CHERRY TC 1200)
egk_w = 65;
egk_h = 120;
egk_d = 15;

// 3. VESA Mount (100x100)
vesa_spacing = 100.0;
vesa_hole_r = 2.2; // M4 Kernloch/Bohrung für Gewindeeinsätze
vesa_reinforcement = 5.0; // Zusätzliche Dicke für die VESA-Platte

// --- Berechnete Dimensionen ---
outer_w = tablet_w + tol + (wall_th * 2);
outer_h = tablet_h + tol + (wall_th * 2);
outer_d = tablet_d + tol + face_th + wall_th;

// Modus-Steuerung zum Slicen in Bambu Studio
// Optionen: "full", "left_half", "right_half", "egk_dock"
render_mode = "full"; 

// ==========================================
// Hauptmodule
// ==========================================

module tablet_case_back() {
    difference() {
        // Grundkörper (Rückwand + Seitenwände)
        union() {
            cube([outer_w, outer_h, outer_d - face_th]);
            
            // VESA Verstärkung mittig
            translate([(outer_w - 120)/2, (outer_h - 120)/2, -vesa_reinforcement])
                cube([120, 120, vesa_reinforcement]);
        }
        
        // Innerer Ausschnitt für das Tablet
        translate([wall_th, wall_th, wall_th])
            cube([tablet_w + tol, tablet_h + tol, outer_d]);
            
        // VESA Bohrungen (100x100 M4)
        translate([outer_w/2, outer_h/2, -vesa_reinforcement - 1]) {
            for(x = [-vesa_spacing/2, vesa_spacing/2]) {
                for(y = [-vesa_spacing/2, vesa_spacing/2]) {
                    translate([x, y, 0])
                        cylinder(r=vesa_hole_r, h=outer_d + vesa_reinforcement + 2);
                }
            }
        }
        
        // Lüftungsschlitze (Thermik)
        for(i = [1 : 8]) {
            translate([outer_w/2 - 60, outer_h/2 - 70 + (i*15), -1])
                cube([120, 5, wall_th + 2]);
        }
        
        // Lade-Kabelauslass (Seitlich rechts)
        translate([outer_w - wall_th - 1, outer_h/2 - 10, wall_th])
            cube([wall_th + 2, 20, tablet_d + tol]);
    }
}

module tablet_face_plate() {
    difference() {
        // Faceplate Grundkörper
        cube([outer_w, outer_h, face_th]);
        
        // Display-Ausschnitt (zentriert)
        translate([(outer_w - display_w)/2, (outer_h - display_h)/2, -1])
            cube([display_w, display_h, face_th + 2]);
            
        // Kamera-Aussparung (oben mittig)
        translate([outer_w/2, outer_h - wall_th - 5, -1])
            cylinder(r=4, h=face_th + 2);
    }
}

module egk_dock() {
    dock_wall = 3.0;
    dock_outer_w = egk_w + tol + (dock_wall * 2);
    dock_outer_h = egk_h + tol + (dock_wall * 2);
    dock_outer_d = egk_d + tol + dock_wall;
    
    difference() {
        // Dock Grundkörper
        cube([dock_outer_w, dock_outer_h, dock_outer_d]);
        
        // Innerer Ausschnitt für Cherry TC 1200
        translate([dock_wall, dock_wall, dock_wall])
            cube([egk_w + tol, egk_h + tol + 10, egk_d + tol]); // +10h für Einschuböffnung oben
            
        // Kabelführungsschlitz unten nach hinten
        translate([dock_outer_w/2 - 10, -1, -1])
            cube([20, dock_wall + 5, dock_wall + 2]);
            
        // Karten-Einschubschlitz vorne (Bedienseite)
        translate([dock_wall + 10, dock_outer_h - dock_wall - 1, dock_wall + 5])
            cube([egk_w - 20 + tol, dock_wall + 2, 5]);
    }
}

// ==========================================
// Schwalbenschwanz / Modulare Splittung
// ==========================================

// Da das iPad (248.6mm) + Wände (8mm) = 256.6mm beträgt, 
// überschreitet es minimal das Bambu Bett (256x256mm).
// Wir schneiden es mit einem Zickzack-Verbinder in der Mitte durch.

module full_case() {
    union() {
        tablet_case_back();
        translate([0, 0, outer_d - face_th])
            tablet_face_plate();
    }
}

module split_left() {
    intersection() {
        full_case();
        // Schnitt bei X = Hälfte
        cube([outer_w/2 + 5, outer_h, outer_d * 2]);
    }
}

module split_right() {
    intersection() {
        full_case();
        translate([outer_w/2 + 5, 0, 0])
            cube([outer_w/2, outer_h, outer_d * 2]);
    }
}

// ==========================================
// Rendering-Auswahl
// ==========================================

if (render_mode == "full") {
    // 1. Tablet Gehäuse
    full_case();
    
    // 2. eGK Dock (angebaut an der rechten Seite)
    translate([outer_w, 20, 0])
        egk_dock();
} 
else if (render_mode == "left_half") {
    split_left();
} 
else if (render_mode == "right_half") {
    split_right();
} 
else if (render_mode == "egk_dock") {
    egk_dock();
}
