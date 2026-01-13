/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        './Views/**/*.cshtml',
        './Areas/**/Views/**/*.cshtml'
    ],
    theme: {
        extend: {
            colors: {
                primary: {
                    DEFAULT: '#22C55E',   // «·√Œ÷— «·√”«”Ì (Green 500)
                    light: '#4ADE80',     // «·√Œ÷— «·›« Õ (Green 400)s
                    dark: '#16A34A',       // «·√Œ÷— «·€«„ﬁ (Green 600)
                    100: '#DCFCE7',
                    200: '#BBF7D0',
                    300: '#86EFAC',
                    400: '#4ADE80',
                    500: '#22C55E',
                    600: '#16A34A',
                    700: '#15803D',
                    800: '#166534',
                    900: '#14532D',
                },
                secondary: {
                    DEFAULT: '#9CA3AF',   // —„«œÌ „ Ê”ÿ (Gray 400)
                    light: '#D1D5DB',     // —„«œÌ ›« Õ (Gray 300)
                    dark: '#6B7280'       // —„«œÌ €«„ﬁ (Gray 500)
                }
            },
            fontFamily: {
                sans: ['Montserrat', 'ui-sans-serif', 'system-ui'],
            },
        }
    },
    plugins: [],
}
