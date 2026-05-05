/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./Views/**/*.cshtml", "./Areas/**/Views/**/*.cshtml"],
  theme: {
    extend: {
      colors: {
        primary: {
          // Tints (الدرجات الفاتحة)
          50: "#F0FDF4",
          100: "#DCFCE7",
          200: "#BBF7D0",
          300: "#86EFAC",
          400: "#4ADE80",
          light: "#4ADE80", // مطابق لدرجة 400

          // Base & Saturation (اللون الأساسي)
          500: "#22C55E",
          DEFAULT: "#22C55E", // Green 500

          // Shades (الدرجات الغامقة)
          600: "#16A34A",
          dark: "#16A34A", // مطابق لدرجة 600
          700: "#15803D",
          800: "#166534",
          900: "#14532D",
          950: "#052E16",
        },
        secondary: {
          100: "#F3F4F6",
          200: "#E5E7EB",
          300: "#D1D5DB",
          light: "#D1D5DB", // Gray 300
          400: "#9CA3AF",
          DEFAULT: "#9CA3AF", // Gray 400
          500: "#6B7280",
          dark: "#6B7280", // Gray 500
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
