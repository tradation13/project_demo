/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./Views/**/*.cshtml", "./Areas/**/Views/**/*.cshtml"],
  theme: {
    extend: {
      colors: {
        // اللون الأخضر الأساسي (PhysioTech Theme)
        primary: {
          50: "#F0FDF4",
          100: "#DCFCE7",
          200: "#BBF7D0",
          300: "#86EFAC",
          400: "#4ADE80",
          light: "#4ADE80",
          500: "#22C55E",
          DEFAULT: "#22C55E",
          600: "#16A34A",
          dark: "#16A34A",
          700: "#15803D",
          800: "#166534",
          900: "#14532D",
          950: "#052E16",
        },
        // اللون الأزرق الجديد (للتنوع في الواجهات أو التقارير)
        blue: {
          50: "#EFF6FF",
          100: "#DBEAFE",
          200: "#BFDBFE",
          300: "#93C5FD",
          400: "#60A5FA",
          light: "#60A5FA",
          500: "#3B82F6",
          DEFAULT: "#3B82F6",
          600: "#2563EB",
          dark: "#2563EB",
          700: "#1D4ED8",
          800: "#1E40AF",
          900: "#1E3A8A",
          950: "#172554",
        },
        // اللون الرمادي (للخلفيات والنصوص الثانوية)
        secondary: {
          100: "#F3F4F6",
          200: "#E5E7EB",
          300: "#D1D5DB",
          light: "#D1D5DB",
          400: "#9CA3AF",
          DEFAULT: "#9CA3AF",
          500: "#6B7280",
          dark: "#6B7280",
          600: "#4B5563",
          700: "#374151",
          800: "#1F2937",
          900: "#111827",
        },
      },
      fontFamily: {
        sans: ["Montserrat", "ui-sans-serif", "system-ui"],
      },
    },
  },
  plugins: [],
};
