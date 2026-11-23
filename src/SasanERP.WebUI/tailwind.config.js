/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        sasan: {
          canvas: '#080E1A',
          surface: '#0F172A',
          card: '#131E33',
          border: '#1E293B',
          cyan: '#00E5FF',
          gold: '#F59E0B',
          text: '#F8FAFC',
          muted: '#8EA4C1',
        }
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
        mono: ['JetBrains Mono', 'Consolas', 'monospace'],
      }
    },
  },
  plugins: [],
};
