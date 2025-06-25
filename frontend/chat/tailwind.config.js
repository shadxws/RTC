/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./src/**/*.{js,jsx,ts,tsx}"],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        primary: {
          light: '#3B82F6',
          dark: '#60A5FA',
        },
        background: {
          light: '#FFFFFF',
          dark: '#1A1A1A',
        },
        text: {
          light: '#1F2937',
          dark: '#F3F4F6',
        }
      }
    },
  },
  plugins: [
    require('tailwind-scrollbar-hide'),
    require('tailwind-scrollbar'),
  ],
};
